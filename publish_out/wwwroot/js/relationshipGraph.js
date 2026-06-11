// Граф связей на canvas: физика, перетаскивание и отрисовка живут в JavaScript
// и работают через requestAnimationFrame — Blazor не перерисовывает DOM на каждый кадр,
// поэтому граф остаётся плавным при любом количестве персонажей.
window.charlaiuGraph = (() => {
  const WORLD_WIDTH = 800;
  const WORLD_HEIGHT = 520;
  const NODE_RADIUS = 30;
  const GOLDEN_ANGLE_RADIANS = 2.399963;

  // Физические константы — те же, что были в C#-версии
  const REPULSION_STRENGTH = 80000;
  const SPRING_REST_LENGTH = 235;
  const SPRING_STIFFNESS = 0.025;
  const CENTERING_STRENGTH = 0.012;
  const VELOCITY_DAMPING = 0.80;
  const SLEEP_VELOCITY_THRESHOLD = 0.02;

  let canvasElement = null;
  let renderingContext = null;
  let dotNetReference = null;
  let animationFrameHandle = 0;
  let resizeObserver = null;

  /** id → {x, y, vx, vy, label, filtered, pinned} */
  const nodesById = new Map();
  let edges = [];
  let spawnedNodesCounter = 0;

  let draggedNodeId = null;
  let pointerWasDragged = false;
  let simulationIsAsleep = false;

  /** Цвета берутся из CSS-переменных активной темы при каждой отрисовке. */
  function readThemeColors() {
    const style = getComputedStyle(document.documentElement);
    return {
      surface: style.getPropertyValue("--surface-color").trim() || "#ffffff",
      background: style.getPropertyValue("--background-color").trim() || "#faf7f2",
      text: style.getPropertyValue("--text-color").trim() || "#2b2a27",
      accent: style.getPropertyValue("--accent-color").trim() || "#2f6f6a"
    };
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
    for (const [nodeId, node] of nodesById) {
      const deltaX = worldPoint.x - node.x;
      const deltaY = worldPoint.y - node.y;
      if (deltaX * deltaX + deltaY * deltaY <= NODE_RADIUS * NODE_RADIUS) {
        foundNodeId = nodeId; // последний в порядке отрисовки — визуально верхний
      }
    }
    return foundNodeId;
  }

  function wakeSimulation() { simulationIsAsleep = false; }

  // ----- Физика -----

  function applySimulationStep() {
    const nodes = [...nodesById.values()];

    for (let firstIndex = 0; firstIndex < nodes.length; firstIndex++) {
      for (let secondIndex = firstIndex + 1; secondIndex < nodes.length; secondIndex++) {
        const firstNode = nodes[firstIndex];
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
      const sourceNode = nodesById.get(edge.a);
      const targetNode = nodesById.get(edge.b);
      if (!sourceNode || !targetNode) { continue; }

      const deltaX = targetNode.x - sourceNode.x;
      const deltaY = targetNode.y - sourceNode.y;
      const distance = Math.max(Math.sqrt(deltaX * deltaX + deltaY * deltaY), 1);
      const springForce = (distance - SPRING_REST_LENGTH) * SPRING_STIFFNESS;
      const forceX = springForce * deltaX / distance;
      const forceY = springForce * deltaY / distance;

      sourceNode.vx += forceX; sourceNode.vy += forceY;
      targetNode.vx -= forceX; targetNode.vy -= forceY;
    }

    let totalKineticEnergy = 0;
    for (const node of nodesById.values()) {
      if (node.pinned) { node.vx = 0; node.vy = 0; continue; }

      node.vx += (WORLD_WIDTH / 2 - node.x) * CENTERING_STRENGTH;
      node.vy += (WORLD_HEIGHT / 2 - node.y) * CENTERING_STRENGTH;
      node.vx *= VELOCITY_DAMPING;
      node.vy *= VELOCITY_DAMPING;
      node.x = Math.min(Math.max(node.x + node.vx, 45), WORLD_WIDTH - 45);
      node.y = Math.min(Math.max(node.y + node.vy, 45), WORLD_HEIGHT - 60);

      totalKineticEnergy += node.vx * node.vx + node.vy * node.vy;
    }

    // Уснувший граф не считается и не перерисовывается — ноль нагрузки в покое
    if (totalKineticEnergy < SLEEP_VELOCITY_THRESHOLD && draggedNodeId === null) {
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

  function drawFrame() {
    const theme = readThemeColors();
    renderingContext.clearRect(0, 0, WORLD_WIDTH, WORLD_HEIGHT);

    // Рёбра: параллельные связи одной пары расходятся дугами от канонической нормали пары
    const edgesByPairKey = new Map();
    for (const edge of edges) {
      if (!nodesById.has(edge.a) || !nodesById.has(edge.b)) { continue; }
      const pairKey = edge.a < edge.b ? edge.a + "|" + edge.b : edge.b + "|" + edge.a;
      if (!edgesByPairKey.has(pairKey)) { edgesByPairKey.set(pairKey, []); }
      edgesByPairKey.get(pairKey).push(edge);
    }

    const labelDrawQueue = [];

    for (const [pairKey, parallelEdges] of edgesByPairKey) {
      const [canonicalFirstId, canonicalSecondId] = pairKey.split("|");
      const canonicalFirstNode = nodesById.get(canonicalFirstId);
      const canonicalSecondNode = nodesById.get(canonicalSecondId);

      const canonicalDeltaX = canonicalSecondNode.x - canonicalFirstNode.x;
      const canonicalDeltaY = canonicalSecondNode.y - canonicalFirstNode.y;
      const canonicalDistance = Math.max(Math.hypot(canonicalDeltaX, canonicalDeltaY), 1);
      const normalX = -canonicalDeltaY / canonicalDistance;
      const normalY = canonicalDeltaX / canonicalDistance;

      for (let parallelIndex = 0; parallelIndex < parallelEdges.length; parallelIndex++) {
        const edge = parallelEdges[parallelIndex];
        const sourceNode = nodesById.get(edge.a);
        const targetNode = nodesById.get(edge.b);

        const arcOffset = (parallelIndex - (parallelEdges.length - 1) / 2.0) * 62;
        const controlX = (sourceNode.x + targetNode.x) / 2 + normalX * arcOffset * 2;
        const controlY = (sourceNode.y + targetNode.y) / 2 + normalY * arcOffset * 2;

        renderingContext.beginPath();
        renderingContext.moveTo(sourceNode.x, sourceNode.y);
        renderingContext.quadraticCurveTo(controlX, controlY, targetNode.x, targetNode.y);
        renderingContext.strokeStyle = edge.color;
        renderingContext.lineWidth = 2.5;
        renderingContext.stroke();

        if (edge.oneWay) {
          const tangentX = targetNode.x - controlX;
          const tangentY = targetNode.y - controlY;
          const tangentLength = Math.max(Math.hypot(tangentX, tangentY), 1);
          const unitX = tangentX / tangentLength;
          const unitY = tangentY / tangentLength;
          const tipX = targetNode.x - unitX * 33;
          const tipY = targetNode.y - unitY * 33;
          const baseX = tipX - unitX * 12;
          const baseY = tipY - unitY * 12;
          const sideX = -unitY * 6;
          const sideY = unitX * 6;

          renderingContext.beginPath();
          renderingContext.moveTo(tipX, tipY);
          renderingContext.lineTo(baseX + sideX, baseY + sideY);
          renderingContext.lineTo(baseX - sideX, baseY - sideY);
          renderingContext.closePath();
          renderingContext.fillStyle = edge.color;
          renderingContext.fill();
        }

        // Подписи рисуются после всех линий, чтобы подложка перекрывала их
        labelDrawQueue.push({
          text: edge.label,
          x: 0.25 * sourceNode.x + 0.5 * controlX + 0.25 * targetNode.x,
          y: 0.25 * sourceNode.y + 0.5 * controlY + 0.25 * targetNode.y - 7,
          font: "12.5px Georgia, serif",
          color: edge.color
        });
      }
    }

    for (const node of nodesById.values()) {
      renderingContext.beginPath();
      renderingContext.arc(node.x, node.y, NODE_RADIUS, 0, Math.PI * 2);
      renderingContext.fillStyle = theme.background;
      renderingContext.fill();
      renderingContext.strokeStyle = theme.accent;
      renderingContext.lineWidth = node.filtered ? 4 : 2;
      renderingContext.stroke();

      // Подписи персонажей — в том же стиле с подложкой, что и подписи связей
      labelDrawQueue.push({
        text: node.label, x: node.x, y: node.y + 48,
        font: "15px Georgia, serif", color: theme.text
      });
    }

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

  // ----- Обработка мыши -----

  function handlePointerDown(pointerEvent) {
    if (pointerEvent.button !== 0) { return; }
    const nodeId = findNodeAt(toWorldCoordinates(pointerEvent));
    if (nodeId === null) { return; }

    draggedNodeId = nodeId;
    pointerWasDragged = false;
    nodesById.get(nodeId).pinned = true;
    canvasElement.setPointerCapture(pointerEvent.pointerId);
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

    canvasElement.style.cursor = findNodeAt(toWorldCoordinates(pointerEvent)) !== null ? "grab" : "default";
  }

  function handlePointerUp() {
    if (draggedNodeId !== null) {
      const draggedNode = nodesById.get(draggedNodeId);
      if (draggedNode) { draggedNode.pinned = false; }
      draggedNodeId = null;
      wakeSimulation();
    }
  }

  /**
   * Ставит контекстное меню под курсор. Строгая CSP запрещает инлайновые
   * атрибуты style, поэтому позиция задаётся через CSSOM после рендера Blazor.
   */
  function positionContextMenuWhenRendered(clientX, clientY, attemptsLeft) {
    const menuElement = document.querySelector(".node-context-menu");
    if (!menuElement) {
      if (attemptsLeft > 0) {
        requestAnimationFrame(() => positionContextMenuWhenRendered(clientX, clientY, attemptsLeft - 1));
      }
      return;
    }

    // Меню не выходит за края окна
    const menuRect = menuElement.getBoundingClientRect();
    const clampedX = Math.min(clientX, window.innerWidth - menuRect.width - 8);
    const clampedY = Math.min(clientY, window.innerHeight - menuRect.height - 8);
    menuElement.style.left = Math.max(8, clampedX) + "px";
    menuElement.style.top = Math.max(8, clampedY) + "px";
    menuElement.style.visibility = "visible";
  }

  function handleContextMenu(pointerEvent) {
    pointerEvent.preventDefault();
    const nodeId = findNodeAt(toWorldCoordinates(pointerEvent));
    if (nodeId !== null && dotNetReference) {
      dotNetReference
        .invokeMethodAsync("ShowNodeContextMenuFromGraph", nodeId, pointerEvent.clientX, pointerEvent.clientY)
        .then(() => positionContextMenuWhenRendered(pointerEvent.clientX, pointerEvent.clientY, 10));
    }
  }

  function handleClick(pointerEvent) {
    // Простой клик (без перетаскивания) закрывает контекстное меню
    if (!pointerWasDragged && dotNetReference) {
      dotNetReference.invokeMethodAsync("CloseNodeContextMenuFromGraph");
    }
    pointerWasDragged = false;
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
      canvasElement.addEventListener("contextmenu", handleContextMenu);
      canvasElement.addEventListener("click", handleClick);

      animationLoop();
    },

    /**
     * Обновляет данные графа. Узлы получают позиции по «золотому углу»,
     * уже существующие узлы сохраняют свои координаты и скорость.
     * @param {Array<{id:string,label:string,filtered:boolean}>} graphNodes
     * @param {Array<{a:string,b:string,label:string,color:string,oneWay:boolean}>} graphEdges
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
            pinned: false
          });
        }
      }

      edges = graphEdges;
      wakeSimulation();
    },

    /** Будит симуляцию — например, после смены темы оформления. */
    refresh() { wakeSimulation(); },

    /** Останавливает анимацию и снимает обработчики. */
    dispose() {
      if (animationFrameHandle) { cancelAnimationFrame(animationFrameHandle); animationFrameHandle = 0; }
      if (resizeObserver) { resizeObserver.disconnect(); resizeObserver = null; }
      if (canvasElement) {
        canvasElement.removeEventListener("pointerdown", handlePointerDown);
        canvasElement.removeEventListener("pointermove", handlePointerMove);
        canvasElement.removeEventListener("pointerup", handlePointerUp);
        canvasElement.removeEventListener("pointercancel", handlePointerUp);
        canvasElement.removeEventListener("contextmenu", handleContextMenu);
        canvasElement.removeEventListener("click", handleClick);
      }
      canvasElement = null;
      renderingContext = null;
      dotNetReference = null;
      nodesById.clear();
      edges = [];
      spawnedNodesCounter = 0;
      draggedNodeId = null;
    }
  };
})();
