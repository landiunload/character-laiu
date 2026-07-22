// Граф связей на canvas: физика, перетаскивание и отрисовка живут в JavaScript
// и работают через requestAnimationFrame — Blazor не перерисовывает DOM на каждый кадр,
// поэтому граф остаётся плавным при любом количестве персонажей.
window.charlaiuGraph = (() => {
  const WORLD_WIDTH = 800;
  const WORLD_HEIGHT = 520;
  const NODE_RADIUS = 30;
  const GOLDEN_ANGLE_RADIANS = 2.399963;

  // Физические константы
  const REPULSION_STRENGTH = 80000;        // отталкивание узлов друг от друга
  const SPRING_REST_LENGTH = 235;          // желаемая длина связи
  const SPRING_STIFFNESS = 0.025;          // жёсткость пружины-связи
  const CENTERING_STRENGTH = 0.012;        // притяжение к центру холста
  const VELOCITY_DAMPING = 0.80;           // затухание скоростей
  const EDGE_NODE_REPULSION = 26000;       // отталкивание узлов от чужих линий связи
  const EDGE_NODE_RANGE = 70;              // дальше этого расстояния линия на узел не давит
  const SLEEP_VELOCITY_THRESHOLD = 0.02;
  const HOVER_ANIMATION_SPEED = 0.22;      // скорость анимаций наведения (доля пути за кадр)

  let canvasElement = null;
  let renderingContext = null;
  let dotNetReference = null;
  let animationFrameHandle = 0;
  let resizeObserver = null;

  /** id → {x, y, vx, vy, label, filtered, pinned, scale, targetScale} */
  const nodesById = new Map();
  /** [{id, a, b, label, color, oneWay, hoverT, geometry}] — порядок задаёт раскладку дуг */
  let edges = [];
  let spawnedNodesCounter = 0;

  // Плоские снимки Map для горячих циклов: обход массива не создаёт пар [ключ, значение]
  // на каждой итерации, в отличие от перебора самой Map. Пересобираются только в setData.
  let nodeIds = [];
  let nodeList = [];

  let draggedNodeId = null;
  let hoveredNodeId = null;
  let hoveredEdgeId = null;
  let pointerWasDragged = false;
  let simulationIsAsleep = false;

  /**
   * Цвета активной темы. getComputedStyle заставляет браузер пересчитать стили,
   * поэтому результат кешируется: тема меняется редко, а кадров — 60 в секунду.
   * Кеш сбрасывает refresh(), который вызывается после смены темы.
   */
  let cachedThemeColors = null;

  function readThemeColors() {
    if (cachedThemeColors !== null) { return cachedThemeColors; }

    const style = getComputedStyle(document.documentElement);
    cachedThemeColors = {
      surface: style.getPropertyValue("--surface-color").trim() || "#ffffff",
      background: style.getPropertyValue("--background-color").trim() || "#faf7f2",
      text: style.getPropertyValue("--text-color").trim() || "#2b2a27",
      accent: style.getPropertyValue("--accent-color").trim() || "#2f6f6a"
    };
    return cachedThemeColors;
  }

  /** Перевод координат события мыши в «мировые» координаты холста 800×520. */
  function toWorldCoordinates(mouseEvent) {
    const boundingRect = canvasElement.getBoundingClientRect();
    return {
      x: (mouseEvent.clientX - boundingRect.left) * (WORLD_WIDTH / boundingRect.width),
      y: (mouseEvent.clientY - boundingRect.top) * (WORLD_HEIGHT / boundingRect.height)
    };
  }

  function findNodeAt(worldPoint) {
    let foundNodeId = null;
    for (let nodeIndex = 0; nodeIndex < nodeList.length; nodeIndex++) {
      const node = nodeList[nodeIndex];
      const deltaX = worldPoint.x - node.x;
      const deltaY = worldPoint.y - node.y;
      const hitRadius = NODE_RADIUS * (node.scale || 1);
      if (deltaX * deltaX + deltaY * deltaY <= hitRadius * hitRadius) {
        foundNodeId = nodeIds[nodeIndex]; // последний в порядке отрисовки — визуально верхний
      }
    }
    return foundNodeId;
  }

  /**
   * Точка квадратичной кривой Безье при параметре t.
   * Результат кладётся в общий буфер, а не в новый объект: функция зовётся
   * тысячи раз за кадр (физика, попадание курсора), и мусор от неё был бы
   * основным источником пауз сборщика. Читать сразу, не сохраняя ссылку.
   */
  const curvePointBuffer = { x: 0, y: 0 };

  function quadraticPoint(geometry, t) {
    const inverseT = 1 - t;
    const squaredInverseT = inverseT * inverseT;
    const doubleCross = 2 * inverseT * t;
    const squaredT = t * t;
    curvePointBuffer.x = squaredInverseT * geometry.sourceX + doubleCross * geometry.controlX + squaredT * geometry.targetX;
    curvePointBuffer.y = squaredInverseT * geometry.sourceY + doubleCross * geometry.controlY + squaredT * geometry.targetY;
    return curvePointBuffer;
  }

  /** Поиск связи под курсором: расстояние до выборки точек кривой. */
  function findEdgeAt(worldPoint) {
    const hitDistanceSquared = 9 * 9;
    let foundEdgeId = null;

    for (const edge of edges) {
      if (!edge.geometry.isValid) { continue; }
      for (let sampleIndex = 1; sampleIndex < 16; sampleIndex++) {
        const curvePoint = quadraticPoint(edge.geometry, sampleIndex / 16);
        const deltaX = worldPoint.x - curvePoint.x;
        const deltaY = worldPoint.y - curvePoint.y;
        if (deltaX * deltaX + deltaY * deltaY <= hitDistanceSquared) {
          foundEdgeId = edge.id;
          break;
        }
      }
    }
    return foundEdgeId;
  }

  function wakeSimulation() { simulationIsAsleep = false; }

  // ----- Геометрия рёбер (общая для физики, отрисовки и попадания курсора) -----

  /**
   * Раскладка дуг: параллельные связи одной пары расходятся в обе стороны от
   * канонической нормали пары, а порядок связей в списке решает, какая дуга
   * окажется ближе к прямой. Состав пар меняется только вместе с данными графа,
   * поэтому группировка считается один раз в setData, а не 120 раз в секунду.
   */
  function rebuildEdgeLayout() {
    const edgesByPairKey = new Map();

    for (const edge of edges) {
      edge.geometry.isValid = nodesById.has(edge.a) && nodesById.has(edge.b);
      if (!edge.geometry.isValid) { continue; }

      const pairKey = edge.a < edge.b ? edge.a + "|" + edge.b : edge.b + "|" + edge.a;
      let parallelEdges = edgesByPairKey.get(pairKey);
      if (parallelEdges === undefined) {
        parallelEdges = [];
        edgesByPairKey.set(pairKey, parallelEdges);
      }
      parallelEdges.push(edge);
    }

    for (const parallelEdges of edgesByPairKey.values()) {
      // Нормаль пары считается от меньшего идентификатора к большему,
      // чтобы встречные связи расходились в разные стороны, а не совпадали
      const [canonicalFirstEdge] = parallelEdges;
      const canonicalFirstId = canonicalFirstEdge.a < canonicalFirstEdge.b ? canonicalFirstEdge.a : canonicalFirstEdge.b;
      const canonicalSecondId = canonicalFirstEdge.a < canonicalFirstEdge.b ? canonicalFirstEdge.b : canonicalFirstEdge.a;

      for (let parallelIndex = 0; parallelIndex < parallelEdges.length; parallelIndex++) {
        const edge = parallelEdges[parallelIndex];
        edge.canonicalFirstNode = nodesById.get(canonicalFirstId);
        edge.canonicalSecondNode = nodesById.get(canonicalSecondId);
        edge.sourceNode = nodesById.get(edge.a);
        edge.targetNode = nodesById.get(edge.b);
        edge.arcOffset = (parallelIndex - (parallelEdges.length - 1) / 2.0) * 124;
      }
    }
  }

  /**
   * Пересчитывает контрольные точки дуг под текущие координаты узлов.
   * Зовётся дважды за кадр (после физики и перед отрисовкой), поэтому только
   * арифметика: ни одного нового объекта, строки или Map.
   */
  function computeEdgeGeometries() {
    for (const edge of edges) {
      const geometry = edge.geometry;
      if (!geometry.isValid) { continue; }

      const canonicalDeltaX = edge.canonicalSecondNode.x - edge.canonicalFirstNode.x;
      const canonicalDeltaY = edge.canonicalSecondNode.y - edge.canonicalFirstNode.y;
      const canonicalDistance = Math.max(Math.hypot(canonicalDeltaX, canonicalDeltaY), 1);
      const arcScale = edge.arcOffset / canonicalDistance;

      const sourceNode = edge.sourceNode;
      const targetNode = edge.targetNode;

      geometry.sourceX = sourceNode.x;
      geometry.sourceY = sourceNode.y;
      geometry.targetX = targetNode.x;
      geometry.targetY = targetNode.y;
      geometry.controlX = (sourceNode.x + targetNode.x) / 2 - canonicalDeltaY * arcScale;
      geometry.controlY = (sourceNode.y + targetNode.y) / 2 + canonicalDeltaX * arcScale;
    }
  }

  // ----- Физика -----

  function applySimulationStep() {
    const nodes = nodeList;
    const nodeCount = nodes.length;

    for (let firstIndex = 0; firstIndex < nodeCount; firstIndex++) {
      const firstNode = nodes[firstIndex];

      for (let secondIndex = firstIndex + 1; secondIndex < nodeCount; secondIndex++) {
        const secondNode = nodes[secondIndex];

        let deltaX = secondNode.x - firstNode.x;
        let deltaY = secondNode.y - firstNode.y;

        // Совпавшие узлы расталкиваются детерминированным сдвигом — иначе сила равна нулю
        if (Math.abs(deltaX) < 0.5 && Math.abs(deltaY) < 0.5) {
          deltaX = 1.5 + firstIndex;
          deltaY = -1.0 - secondIndex;
        }

        const squaredDistance = Math.max(deltaX * deltaX + deltaY * deltaY, 400);
        const distance = Math.sqrt(squaredDistance);
        const repulsionForce = REPULSION_STRENGTH / squaredDistance;
        const forceX = repulsionForce * deltaX / distance;
        const forceY = repulsionForce * deltaY / distance;

        firstNode.vx -= forceX; firstNode.vy -= forceY;
        secondNode.vx += forceX; secondNode.vy += forceY;
      }
    }

    for (const edge of edges) {
      if (!edge.geometry.isValid) { continue; }
      const sourceNode = edge.sourceNode;
      const targetNode = edge.targetNode;

      const deltaX = targetNode.x - sourceNode.x;
      const deltaY = targetNode.y - sourceNode.y;
      const distance = Math.max(Math.sqrt(deltaX * deltaX + deltaY * deltaY), 1);
      const springForce = (distance - SPRING_REST_LENGTH) * SPRING_STIFFNESS;
      const forceX = springForce * deltaX / distance;
      const forceY = springForce * deltaY / distance;

      sourceNode.vx += forceX; sourceNode.vy += forceY;
      targetNode.vx -= forceX; targetNode.vy -= forceY;
    }

    // Узлы отталкиваются от чужих линий связи — линии не ложатся на персонажей
    computeEdgeGeometries();
    const edgeNodeRangeSquared = EDGE_NODE_RANGE * EDGE_NODE_RANGE;

    for (const edge of edges) {
      const geometry = edge.geometry;
      if (!geometry.isValid) { continue; }

      // Дуга не достаёт до узлов за пределами описанного вокруг неё прямоугольника —
      // грубая отбраковка снимает вложенный цикл выборки с большинства пар
      const boundsMinX = Math.min(geometry.sourceX, geometry.targetX, geometry.controlX) - EDGE_NODE_RANGE;
      const boundsMaxX = Math.max(geometry.sourceX, geometry.targetX, geometry.controlX) + EDGE_NODE_RANGE;
      const boundsMinY = Math.min(geometry.sourceY, geometry.targetY, geometry.controlY) - EDGE_NODE_RANGE;
      const boundsMaxY = Math.max(geometry.sourceY, geometry.targetY, geometry.controlY) + EDGE_NODE_RANGE;

      for (let nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++) {
        const node = nodes[nodeIndex];
        if (node === edge.sourceNode || node === edge.targetNode) { continue; }
        if (node.x < boundsMinX || node.x > boundsMaxX || node.y < boundsMinY || node.y > boundsMaxY) { continue; }

        // Ближайшая из выборки точек кривой
        let nearestDeltaX = 0, nearestDeltaY = 0;
        let nearestDistanceSquared = Infinity;
        for (let sampleIndex = 1; sampleIndex < 8; sampleIndex++) {
          const curvePoint = quadraticPoint(geometry, sampleIndex / 8);
          const deltaX = node.x - curvePoint.x;
          const deltaY = node.y - curvePoint.y;
          const distanceSquared = deltaX * deltaX + deltaY * deltaY;
          if (distanceSquared < nearestDistanceSquared) {
            nearestDistanceSquared = distanceSquared;
            nearestDeltaX = deltaX;
            nearestDeltaY = deltaY;
          }
        }

        if (nearestDistanceSquared >= edgeNodeRangeSquared) { continue; }

        const distance = Math.sqrt(Math.max(nearestDistanceSquared, 100));
        const pushForce = EDGE_NODE_REPULSION / (distance * distance * distance);
        node.vx += pushForce * nearestDeltaX;
        node.vy += pushForce * nearestDeltaY;
      }
    }

    let totalKineticEnergy = 0;
    for (let nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++) {
      const node = nodes[nodeIndex];
      if (node.pinned) { node.vx = 0; node.vy = 0; continue; }

      node.vx += (WORLD_WIDTH / 2 - node.x) * CENTERING_STRENGTH;
      node.vy += (WORLD_HEIGHT / 2 - node.y) * CENTERING_STRENGTH;
      node.vx *= VELOCITY_DAMPING;
      node.vy *= VELOCITY_DAMPING;
      node.x = Math.min(Math.max(node.x + node.vx, 45), WORLD_WIDTH - 45);
      node.y = Math.min(Math.max(node.y + node.vy, 45), WORLD_HEIGHT - 60);

      totalKineticEnergy += node.vx * node.vx + node.vy * node.vy;
    }

    // Плавные анимации наведения и перетаскивания
    let animationsAreSettled = true;
    for (let nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++) {
      const node = nodes[nodeIndex];
      const nodeId = nodeIds[nodeIndex];
      node.targetScale = nodeId === draggedNodeId ? 1.18 : nodeId === hoveredNodeId ? 1.1 : 1;
      node.scale += (node.targetScale - node.scale) * HOVER_ANIMATION_SPEED;
      if (Math.abs(node.targetScale - node.scale) > 0.004) { animationsAreSettled = false; }
    }
    for (const edge of edges) {
      const targetHover = edge.id === hoveredEdgeId ? 1 : 0;
      edge.hoverT += (targetHover - edge.hoverT) * HOVER_ANIMATION_SPEED;
      if (Math.abs(targetHover - edge.hoverT) > 0.02) { animationsAreSettled = false; }
    }

    // Уснувший граф не считается и не перерисовывается — ноль нагрузки в покое
    if (totalKineticEnergy < SLEEP_VELOCITY_THRESHOLD && draggedNodeId === null && animationsAreSettled) {
      simulationIsAsleep = true;
    }
  }

  // ----- Отрисовка -----

  /** Текст с подложкой цвета поверхности — единый стиль подписей узлов и связей. */
  function drawHaloedText(text, x, y, font, fillColor, haloColor) {
    renderingContext.font = font;
    renderingContext.textAlign = "center";
    renderingContext.textBaseline = "middle";
    renderingContext.lineJoin = "round";
    renderingContext.strokeStyle = haloColor;
    renderingContext.lineWidth = 4;
    renderingContext.strokeText(text, x, y);
    renderingContext.fillStyle = fillColor;
    renderingContext.fillText(text, x, y);
  }

  /**
   * Раскладка подписей без наложений: подписи связей отталкиваются друг от друга
   * и от подписей персонажей (те остаются на месте) за несколько итераций релаксации.
   */
  function relaxLabelPositions(labels) {
    renderingContext.textAlign = "center";
    for (const label of labels) {
      renderingContext.font = label.font;
      label.halfWidth = renderingContext.measureText(label.text).width / 2 + 4;
      label.halfHeight = 9;
    }

    for (let iteration = 0; iteration < 6; iteration++) {
      for (let firstIndex = 0; firstIndex < labels.length; firstIndex++) {
        for (let secondIndex = firstIndex + 1; secondIndex < labels.length; secondIndex++) {
          const firstLabel = labels[firstIndex];
          const secondLabel = labels[secondIndex];

          const overlapX = firstLabel.halfWidth + secondLabel.halfWidth - Math.abs(firstLabel.x - secondLabel.x);
          const overlapY = firstLabel.halfHeight + secondLabel.halfHeight - Math.abs(firstLabel.y - secondLabel.y);
          if (overlapX <= 0 || overlapY <= 0) { continue; }
          if (firstLabel.fixed && secondLabel.fixed) { continue; }

          // Раздвигаются по вертикали — самое естественное направление для строк текста
          const pushDirection = firstLabel.y <= secondLabel.y ? -1 : 1;
          const pushAmount = (overlapY / 2) + 1;

          if (firstLabel.fixed) {
            secondLabel.y -= pushDirection * pushAmount * 2;
          } else if (secondLabel.fixed) {
            firstLabel.y += pushDirection * pushAmount * 2;
          } else {
            firstLabel.y += pushDirection * pushAmount;
            secondLabel.y -= pushDirection * pushAmount;
          }
        }
      }
    }

    for (const label of labels) {
      label.x = Math.min(Math.max(label.x, label.halfWidth + 2), WORLD_WIDTH - label.halfWidth - 2);
      label.y = Math.min(Math.max(label.y, 12), WORLD_HEIGHT - 12);
    }
  }

  function drawFrame() {
    const theme = readThemeColors();
    renderingContext.clearRect(0, 0, WORLD_WIDTH, WORLD_HEIGHT);
    computeEdgeGeometries();

    const labelDrawQueue = [];

    for (const edge of edges) {
      const geometry = edge.geometry;
      if (!geometry.isValid) { continue; }

      // Подсвеченная связь плавно утолщается и становится ярче
      renderingContext.beginPath();
      renderingContext.moveTo(geometry.sourceX, geometry.sourceY);
      renderingContext.quadraticCurveTo(geometry.controlX, geometry.controlY, geometry.targetX, geometry.targetY);
      renderingContext.strokeStyle = edge.color;
      renderingContext.globalAlpha = 0.82 + 0.18 * edge.hoverT;
      renderingContext.lineWidth = 2.5 + 1.8 * edge.hoverT;
      renderingContext.stroke();
      renderingContext.globalAlpha = 1;

      if (edge.oneWay) {
        const tangentX = geometry.targetX - geometry.controlX;
        const tangentY = geometry.targetY - geometry.controlY;
        const tangentLength = Math.max(Math.hypot(tangentX, tangentY), 1);
        const unitX = tangentX / tangentLength;
        const unitY = tangentY / tangentLength;
        const tipX = geometry.targetX - unitX * 33;
        const tipY = geometry.targetY - unitY * 33;
        const baseX = tipX - unitX * 12;
        const baseY = tipY - unitY * 12;
        const sideX = -unitY * (6 + 2 * edge.hoverT);
        const sideY = unitX * (6 + 2 * edge.hoverT);

        renderingContext.beginPath();
        renderingContext.moveTo(tipX, tipY);
        renderingContext.lineTo(baseX + sideX, baseY + sideY);
        renderingContext.lineTo(baseX - sideX, baseY - sideY);
        renderingContext.closePath();
        renderingContext.fillStyle = edge.color;
        renderingContext.fill();
      }

      const labelAnchor = quadraticPoint(geometry, 0.5);
      labelDrawQueue.push({
        text: edge.label,
        x: labelAnchor.x, y: labelAnchor.y - 7,
        font: (edge.hoverT > 0.5 ? "bold " : "") + "12.5px Georgia, serif",
        color: edge.color,
        fixed: false
      });
    }

    for (let nodeIndex = 0; nodeIndex < nodeList.length; nodeIndex++) {
      const node = nodeList[nodeIndex];
      const radius = NODE_RADIUS * node.scale;

      // Перетаскиваемый узел «приподнимается» — мягкая тень под ним
      if (nodeIds[nodeIndex] === draggedNodeId || node.scale > 1.12) {
        renderingContext.shadowColor = "rgba(0, 0, 0, 0.35)";
        renderingContext.shadowBlur = 14 * (node.scale - 1) * 6;
        renderingContext.shadowOffsetY = 3;
      }

      renderingContext.beginPath();
      renderingContext.arc(node.x, node.y, radius, 0, Math.PI * 2);
      renderingContext.fillStyle = theme.background;
      renderingContext.fill();
      renderingContext.shadowColor = "transparent";
      renderingContext.shadowBlur = 0;
      renderingContext.shadowOffsetY = 0;

      renderingContext.strokeStyle = theme.accent;
      renderingContext.lineWidth = (node.filtered ? 4 : 2) + 1.2 * (node.scale - 1) * 8;
      renderingContext.stroke();

      // Подписи персонажей — в том же стиле с подложкой; в раскладке они неподвижны
      labelDrawQueue.push({
        text: node.label, x: node.x, y: node.y + radius + 18,
        font: "15px Georgia, serif", color: theme.text,
        fixed: true
      });
    }

    relaxLabelPositions(labelDrawQueue);
    for (const label of labelDrawQueue) {
      drawHaloedText(label.text, label.x, label.y, label.font, label.color, theme.surface);
    }
  }

  function animationLoop() {
    animationFrameHandle = requestAnimationFrame(animationLoop);
    if (!simulationIsAsleep) {
      applySimulationStep();
      drawFrame();
    }
  }

  /** Холст рисуется в физических пикселях устройства — линии и текст остаются чёткими. */
  function adjustCanvasResolution() {
    const boundingRect = canvasElement.getBoundingClientRect();
    const devicePixelRatioValue = window.devicePixelRatio || 1;
    canvasElement.width = Math.max(1, Math.round(boundingRect.width * devicePixelRatioValue));
    canvasElement.height = Math.max(1, Math.round(boundingRect.width * (WORLD_HEIGHT / WORLD_WIDTH) * devicePixelRatioValue));
    renderingContext.setTransform(
      canvasElement.width / WORLD_WIDTH, 0, 0, canvasElement.height / WORLD_HEIGHT, 0, 0);
    wakeSimulation();
  }

  // ----- Обработка мыши на холсте -----

  function handlePointerDown(pointerEvent) {
    if (pointerEvent.button !== 0) { return; }
    const nodeId = findNodeAt(toWorldCoordinates(pointerEvent));
    if (nodeId === null) { return; }

    draggedNodeId = nodeId;
    pointerWasDragged = false;
    nodesById.get(nodeId).pinned = true;
    canvasElement.setPointerCapture(pointerEvent.pointerId);
    canvasElement.style.cursor = "grabbing";
    wakeSimulation();
    pointerEvent.preventDefault();
  }

  function handlePointerMove(pointerEvent) {
    if (draggedNodeId !== null) {
      const draggedNode = nodesById.get(draggedNodeId);
      if (draggedNode) {
        const worldPoint = toWorldCoordinates(pointerEvent);
        draggedNode.x = Math.min(Math.max(worldPoint.x, 45), WORLD_WIDTH - 45);
        draggedNode.y = Math.min(Math.max(worldPoint.y, 45), WORLD_HEIGHT - 60);
        pointerWasDragged = true;
        wakeSimulation();
      }
      return;
    }

    const worldPoint = toWorldCoordinates(pointerEvent);
    const nodeUnderPointer = findNodeAt(worldPoint);
    const edgeUnderPointer = nodeUnderPointer === null ? findEdgeAt(worldPoint) : null;

    if (nodeUnderPointer !== hoveredNodeId || edgeUnderPointer !== hoveredEdgeId) {
      hoveredNodeId = nodeUnderPointer;
      hoveredEdgeId = edgeUnderPointer;
      wakeSimulation();
    }

    canvasElement.style.cursor =
      nodeUnderPointer !== null ? "grab" : edgeUnderPointer !== null ? "pointer" : "default";
  }

  function handlePointerUp() {
    if (draggedNodeId !== null) {
      const draggedNode = nodesById.get(draggedNodeId);
      if (draggedNode) { draggedNode.pinned = false; }
      draggedNodeId = null;
      canvasElement.style.cursor = "default";
      wakeSimulation();
    }
  }

  function handlePointerLeave() {
    if (hoveredNodeId !== null || hoveredEdgeId !== null) {
      hoveredNodeId = null;
      hoveredEdgeId = null;
      wakeSimulation();
    }
  }

  function handleContextMenu(pointerEvent) {
    pointerEvent.preventDefault();
    if (!dotNetReference) { return; }

    const worldPoint = toWorldCoordinates(pointerEvent);
    const nodeId = findNodeAt(worldPoint);

    if (nodeId !== null) {
      dotNetReference
        .invokeMethodAsync("ShowNodeContextMenuFromGraph", nodeId)
        .then(() => window.charlaiuInterop.positionContextMenu(pointerEvent.clientX, pointerEvent.clientY));
      return;
    }

    const edgeId = findEdgeAt(worldPoint);
    if (edgeId !== null) {
      dotNetReference
        .invokeMethodAsync("ShowEdgeContextMenuFromGraph", edgeId)
        .then(() => window.charlaiuInterop.positionContextMenu(pointerEvent.clientX, pointerEvent.clientY));
    }
  }

  function handleClick() {
    // Простой клик (без перетаскивания) закрывает контекстное меню
    if (!pointerWasDragged && dotNetReference) {
      dotNetReference.invokeMethodAsync("CloseNodeContextMenuFromGraph");
    }
    pointerWasDragged = false;
  }

  // ----- Перетаскивание строк списка связей (настройка порядка дуг) -----

  let listDragSourceId = null;
  let listDragSourceRow = null;
  let listDragTargetRow = null;

  function clearListDragHighlight() {
    if (listDragSourceRow) { listDragSourceRow.classList.remove("dragging"); }
    if (listDragTargetRow) { listDragTargetRow.classList.remove("drag-over"); }
    listDragSourceRow = null;
    listDragTargetRow = null;
  }

  function handleListPointerDown(pointerEvent) {
    const dragHandle = pointerEvent.target.closest ? pointerEvent.target.closest(".drag-handle") : null;
    if (!dragHandle) { return; }
    const rowElement = dragHandle.closest(".relationship-item");
    if (!rowElement || !rowElement.dataset.relationshipId) { return; }

    listDragSourceId = rowElement.dataset.relationshipId;
    listDragSourceRow = rowElement;
    rowElement.classList.add("dragging");
    pointerEvent.preventDefault();
  }

  function handleListPointerMove(pointerEvent) {
    if (listDragSourceId === null) { return; }

    const elementUnderPointer = document.elementFromPoint(pointerEvent.clientX, pointerEvent.clientY);
    const rowUnderPointer = elementUnderPointer && elementUnderPointer.closest
      ? elementUnderPointer.closest(".relationship-item") : null;

    if (rowUnderPointer !== listDragTargetRow) {
      if (listDragTargetRow) { listDragTargetRow.classList.remove("drag-over"); }
      listDragTargetRow = rowUnderPointer !== listDragSourceRow ? rowUnderPointer : null;
      if (listDragTargetRow) { listDragTargetRow.classList.add("drag-over"); }
    }
  }

  function handleListPointerUp() {
    if (listDragSourceId === null) { return; }

    const targetId = listDragTargetRow ? listDragTargetRow.dataset.relationshipId : null;
    const sourceId = listDragSourceId;
    listDragSourceId = null;
    clearListDragHighlight();

    if (targetId && targetId !== sourceId && dotNetReference) {
      dotNetReference.invokeMethodAsync("ReorderRelationshipFromList", sourceId, targetId);
    }
  }

  // ----- Публичный интерфейс -----

  return {
    /** Инициализирует холст и запускает цикл анимации. */
    initialize(canvasId, dotNetObjectReference) {
      this.dispose();

      canvasElement = document.getElementById(canvasId);
      if (!canvasElement) { return; }
      renderingContext = canvasElement.getContext("2d");
      dotNetReference = dotNetObjectReference;

      adjustCanvasResolution();
      resizeObserver = new ResizeObserver(adjustCanvasResolution);
      resizeObserver.observe(canvasElement);

      canvasElement.addEventListener("pointerdown", handlePointerDown);
      canvasElement.addEventListener("pointermove", handlePointerMove);
      canvasElement.addEventListener("pointerup", handlePointerUp);
      canvasElement.addEventListener("pointercancel", handlePointerUp);
      canvasElement.addEventListener("pointerleave", handlePointerLeave);
      canvasElement.addEventListener("contextmenu", handleContextMenu);
      canvasElement.addEventListener("click", handleClick);

      document.addEventListener("pointerdown", handleListPointerDown);
      document.addEventListener("pointermove", handleListPointerMove);
      document.addEventListener("pointerup", handleListPointerUp);

      animationLoop();
    },

    /**
     * Обновляет данные графа. Узлы получают позиции по «золотому углу»,
     * уже существующие узлы сохраняют свои координаты и скорость.
     * @param {Array<{id:string,label:string,filtered:boolean}>} graphNodes
     * @param {Array<{id:string,a:string,b:string,label:string,color:string,oneWay:boolean}>} graphEdges
     */
    setData(graphNodes, graphEdges) {
      const incomingIds = new Set(graphNodes.map(node => node.id));
      for (const existingId of [...nodesById.keys()]) {
        if (!incomingIds.has(existingId)) { nodesById.delete(existingId); }
      }

      for (const incomingNode of graphNodes) {
        const existingNode = nodesById.get(incomingNode.id);
        if (existingNode) {
          existingNode.label = incomingNode.label;
          existingNode.filtered = incomingNode.filtered;
        } else {
          const angleRadians = GOLDEN_ANGLE_RADIANS * spawnedNodesCounter++;
          nodesById.set(incomingNode.id, {
            x: WORLD_WIDTH / 2 + 160 * Math.cos(angleRadians),
            y: WORLD_HEIGHT / 2 + 140 * Math.sin(angleRadians),
            vx: 0, vy: 0,
            label: incomingNode.label,
            filtered: incomingNode.filtered,
            pinned: false,
            scale: 1, targetScale: 1
          });
        }
      }

      // Снимки Map для горячих циклов физики и отрисовки
      nodeIds = [...nodesById.keys()];
      nodeList = [...nodesById.values()];

      // Состояние подсветки переживает обновление данных
      const previousHover = new Map(edges.map(edge => [edge.id, edge.hoverT]));
      edges = graphEdges.map(edge => ({
        ...edge,
        hoverT: previousHover.get(edge.id) || 0,
        // Буфер геометрии живёт столько же, сколько связь: кадр только переписывает числа
        geometry: { isValid: false, sourceX: 0, sourceY: 0, targetX: 0, targetY: 0, controlX: 0, controlY: 0 },
        canonicalFirstNode: null, canonicalSecondNode: null,
        sourceNode: null, targetNode: null, arcOffset: 0
      }));
      rebuildEdgeLayout();
      wakeSimulation();
    },

    /** Будит симуляцию и перечитывает цвета — например, после смены темы оформления. */
    refresh() {
      cachedThemeColors = null;
      wakeSimulation();
    },

    /** Останавливает анимацию и снимает обработчики. */
    dispose() {
      if (animationFrameHandle) { cancelAnimationFrame(animationFrameHandle); animationFrameHandle = 0; }
      if (resizeObserver) { resizeObserver.disconnect(); resizeObserver = null; }
      if (canvasElement) {
        canvasElement.removeEventListener("pointerdown", handlePointerDown);
        canvasElement.removeEventListener("pointermove", handlePointerMove);
        canvasElement.removeEventListener("pointerup", handlePointerUp);
        canvasElement.removeEventListener("pointercancel", handlePointerUp);
        canvasElement.removeEventListener("pointerleave", handlePointerLeave);
        canvasElement.removeEventListener("contextmenu", handleContextMenu);
        canvasElement.removeEventListener("click", handleClick);
      }
      document.removeEventListener("pointerdown", handleListPointerDown);
      document.removeEventListener("pointermove", handleListPointerMove);
      document.removeEventListener("pointerup", handleListPointerUp);
      listDragSourceId = null;
      clearListDragHighlight();

      canvasElement = null;
      renderingContext = null;
      dotNetReference = null;
      cachedThemeColors = null;
      nodesById.clear();
      nodeIds = [];
      nodeList = [];
      edges = [];
      spawnedNodesCounter = 0;
      draggedNodeId = null;
      hoveredNodeId = null;
      hoveredEdgeId = null;
    }
  };
})();
