const {
  Engine, World, Bodies, Body, Events
} = Matter;

const canvas = document.getElementById("game");
const wrap = document.getElementById("gameWrap");
const dropBtn = document.getElementById("dropBtn");
const restartBtn = document.getElementById("restartBtn");
const ballCountEl = document.getElementById("ballCount");
const collectedEl = document.getElementById("collected");
const statusEl = document.getElementById("status");

let engine;
let ctx;
let W = 420;
let H = 720;
let balls = new Set();
let collected = 0;
let dropped = false;
let raf = null;
let lastTime = performance.now();

let launcherX = 210;
let draggingLauncher = false;

const BALL_R = 7;
const INITIAL_BALLS = 5;
const MAX_BALLS = 500;
const LAUNCH_Y = 58;
const LAUNCH_SPACING = 18;

const COLORS = {
  bg1: "#161e33",
  bg2: "#0a0e18",
  wall: "#3a4154",
  ball: "#ffffff",
  outline: "#111826",
  gate2: "#f4aa24",
  gate3: "#7a5cff",
  gate5: "#69b929",
  jump: "#24b8e7",
  bucket: "#222838",
  pin: "#65708a",
  text: "#ffffff"
};

function resizeCanvas() {
  const rect = wrap.getBoundingClientRect();
  const dpr = Math.max(1, Math.min(2, window.devicePixelRatio || 1));
  canvas.width = Math.floor(rect.width * dpr);
  canvas.height = Math.floor(rect.height * dpr);
  canvas.style.width = rect.width + "px";
  canvas.style.height = rect.height + "px";
  ctx = canvas.getContext("2d");
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

  W = rect.width;
  H = rect.height;
  launcherX = clampLauncherX(launcherX || W / 2);
}

function clampLauncherX(x) {
  const groupHalfWidth = ((INITIAL_BALLS - 1) * LAUNCH_SPACING) / 2 + BALL_R + 12;
  return Math.max(groupHalfWidth + 18, Math.min(W - groupHalfWidth - 18, x));
}

function pointerX(event) {
  const rect = canvas.getBoundingClientRect();
  return event.clientX - rect.left;
}

function addStaticRect(x, y, w, h, angle = 0, label = "wall", extra = {}) {
  const body = Bodies.rectangle(x, y, w, h, {
    isStatic: true,
    angle,
    label,
    ...extra
  });
  World.add(engine.world, body);
  return body;
}

function addSensorRect(x, y, w, h, label, data = {}) {
  const body = Bodies.rectangle(x, y, w, h, {
    isStatic: true,
    isSensor: true,
    label
  });
  Object.assign(body, data);
  World.add(engine.world, body);
  return body;
}

function createBall(x, y, vx = 0, vy = 0) {
  if (balls.size >= MAX_BALLS) return null;

  const ball = Bodies.circle(x, y, BALL_R, {
    restitution: 0.45,
    friction: 0.01,
    frictionAir: 0.002,
    density: 0.0018,
    label: "ball"
  });

  ball.plugin = {
    gateCooldown: 0
  };

  Body.setVelocity(ball, { x: vx, y: vy });
  balls.add(ball);
  World.add(engine.world, ball);
  updateHud();
  return ball;
}

function removeBall(ball) {
  if (!balls.has(ball)) return;
  balls.delete(ball);
  World.remove(engine.world, ball);
  updateHud();
}

function createLevel() {
  const wall = 18;

  addStaticRect(wall / 2, H / 2, wall, H, 0, "wall");
  addStaticRect(W - wall / 2, H / 2, wall, H, 0, "wall");

  addGate(W * 0.22, H * 0.34, W * 0.34, 42, 2);
  addGate(W * 0.73, H * 0.34, W * 0.42, 42, 3);

  for (let i = 0; i < 7; i++) {
    const x = W * 0.2 + (i % 4) * 58 + (Math.floor(i / 4) * 27);
    const y = H * 0.45 + Math.floor(i / 4) * 58;
    const pin = Bodies.circle(x, y, 9, { isStatic: true, label: "pin" });
    World.add(engine.world, pin);
  }

  addStaticRect(W * 0.25, H * 0.61, W * 0.42, 13, -0.38);
  addStaticRect(W * 0.75, H * 0.61, W * 0.42, 13, 0.38);

  addJumpPad(W * 0.26, H * 0.69, W * 0.38, 44, 12);
  addGate(W * 0.75, H * 0.69, W * 0.40, 42, 5);

  addStaticRect(W * 0.33, H * 0.81, W * 0.34, 14, 0.28);
  addStaticRect(W * 0.67, H * 0.81, W * 0.34, 14, -0.28);

  addSensorRect(W / 2, H - 34, W * 0.52, 54, "collector");
  addStaticRect(W * 0.23, H - 38, W * 0.18, 14, -0.45);
  addStaticRect(W * 0.77, H - 38, W * 0.18, 14, 0.45);
}

function addGate(x, y, w, h, multiplier) {
  // The coloured multiplier area is a sensor, not a solid platform.
  // Only the small side rails are physical, so balls can fall through it.
  addStaticRect(x - w / 2, y, 8, h);
  addStaticRect(x + w / 2, y, 8, h);

  const sensor = addSensorRect(x, y, w - 16, h, "gate", { multiplier });
  sensor.renderData = { x, y, w, h, multiplier };
}

function addJumpPad(x, y, w, h, power) {
  addStaticRect(x, y + h / 2 - 4, w, 8);
  const sensor = addSensorRect(x, y, w - 12, h - 8, "jump", { jumpPower: power });
  sensor.renderData = { x, y, w, h, power };
}

function onCollision(event) {
  for (const pair of event.pairs) {
    const a = pair.bodyA;
    const b = pair.bodyB;

    const ball = a.label === "ball" ? a : b.label === "ball" ? b : null;
    const other = ball === a ? b : a;
    if (!ball) continue;

    if (other.label === "gate") {
      multiplyBall(ball, other.multiplier);
    }

    if (other.label === "jump") {
      Body.setVelocity(ball, {
        x: ball.velocity.x,
        y: -Math.abs(other.jumpPower || 12)
      });
    }

    if (other.label === "collector") {
      collected += 1;
      removeBall(ball);
      collectedEl.textContent = collected;
    }
  }
}

function multiplyBall(ball, multiplier) {
  const now = performance.now();
  if (!ball.plugin) ball.plugin = {};
  if (ball.plugin.gateCooldown && now < ball.plugin.gateCooldown) return;

  const x = ball.position.x;
  const y = ball.position.y;
  const vx = ball.velocity.x;
  const vy = Math.max(1, ball.velocity.y);

  removeBall(ball);

  const remaining = Math.min(multiplier, MAX_BALLS - balls.size);
  for (let i = 0; i < remaining; i++) {
    const spread = (i - (remaining - 1) / 2) * 3.4;
    const b = createBall(x + spread, y + 5, vx + spread * 0.055, vy + Math.random() * 0.35);
    if (b) b.plugin.gateCooldown = now + 260;
  }
}

function setup() {
  if (raf) cancelAnimationFrame(raf);

  resizeCanvas();
  launcherX = W / 2;
  draggingLauncher = false;

  engine = Engine.create({
    gravity: { x: 0, y: 1, scale: 0.00135 }
  });

  balls = new Set();
  collected = 0;
  dropped = false;
  collectedEl.textContent = "0";
  statusEl.textContent = "Drag balls left or right";
  dropBtn.disabled = false;
  dropBtn.style.display = "block";
  dropBtn.textContent = "DROP 5 BALLS";

  createLevel();

  Events.on(engine, "collisionStart", onCollision);

  lastTime = performance.now();
  loop(lastTime);
  updateHud();
}

function dropInitialBalls() {
  if (dropped) return;
  dropped = true;
  draggingLauncher = false;
  dropBtn.disabled = true;
  dropBtn.style.display = "none";
  statusEl.textContent = "Running";

  for (let i = 0; i < INITIAL_BALLS; i++) {
    createBall(
      launcherX + (i - (INITIAL_BALLS - 1) / 2) * LAUNCH_SPACING,
      LAUNCH_Y,
      (i - (INITIAL_BALLS - 1) / 2) * 0.05,
      0
    );
  }
}

function updateHud() {
  ballCountEl.textContent = dropped ? balls.size : INITIAL_BALLS;

  if (dropped && balls.size === 0) {
    statusEl.textContent = `Finished — ${collected} collected`;
  }
}

function drawRoundedRect(x, y, w, h, r, fill) {
  ctx.beginPath();
  ctx.roundRect(x, y, w, h, r);
  ctx.fillStyle = fill;
  ctx.fill();
}

function drawLauncher() {
  if (dropped) return;

  ctx.save();
  ctx.setLineDash([5, 7]);
  ctx.strokeStyle = "rgba(255,255,255,.16)";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(launcherX, LAUNCH_Y + 15);
  ctx.lineTo(launcherX, H * 0.30);
  ctx.stroke();
  ctx.setLineDash([]);

  for (let i = 0; i < INITIAL_BALLS; i++) {
    const x = launcherX + (i - (INITIAL_BALLS - 1) / 2) * LAUNCH_SPACING;
    ctx.beginPath();
    ctx.arc(x, LAUNCH_Y, BALL_R, 0, Math.PI * 2);
    ctx.fillStyle = COLORS.ball;
    ctx.fill();
    ctx.lineWidth = 2;
    ctx.strokeStyle = COLORS.outline;
    ctx.stroke();
  }

  ctx.fillStyle = "rgba(255,255,255,.62)";
  ctx.font = "800 11px Arial";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillText("↔ DRAG TO AIM", launcherX, LAUNCH_Y + 28);
  ctx.restore();
}

function draw() {
  const gradient = ctx.createLinearGradient(0, 0, 0, H);
  gradient.addColorStop(0, COLORS.bg1);
  gradient.addColorStop(1, COLORS.bg2);
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, W, H);

  for (const body of engine.world.bodies) {
    if (body.label === "ball") {
      ctx.beginPath();
      ctx.arc(body.position.x, body.position.y, BALL_R, 0, Math.PI * 2);
      ctx.fillStyle = COLORS.ball;
      ctx.fill();
      ctx.lineWidth = 2;
      ctx.strokeStyle = COLORS.outline;
      ctx.stroke();
      continue;
    }

    if (body.label === "pin") {
      ctx.beginPath();
      ctx.arc(body.position.x, body.position.y, 9, 0, Math.PI * 2);
      ctx.fillStyle = COLORS.pin;
      ctx.fill();
      continue;
    }

    if (body.label === "gate") {
      const d = body.renderData;
      const color = d.multiplier === 5 ? COLORS.gate5 : d.multiplier === 3 ? COLORS.gate3 : COLORS.gate2;
      drawRoundedRect(d.x - d.w / 2, d.y - d.h / 2, d.w, d.h, 8, color);
      ctx.fillStyle = COLORS.text;
      ctx.font = "900 28px Arial";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillText("×" + d.multiplier, d.x, d.y + 1);
      continue;
    }

    if (body.label === "jump") {
      const d = body.renderData;
      drawRoundedRect(d.x - d.w / 2, d.y - d.h / 2, d.w, d.h, 8, COLORS.jump);
      ctx.fillStyle = COLORS.text;
      ctx.font = "900 28px Arial";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillText("⇈", d.x, d.y);
      continue;
    }

    if (body.label === "collector") {
      const w = W * 0.52;
      const h = 54;
      drawRoundedRect(W / 2 - w / 2, H - 34 - h / 2, w, h, 10, COLORS.bucket);
      ctx.fillStyle = "#b9c4df";
      ctx.font = "800 14px Arial";
      ctx.textAlign = "center";
      ctx.fillText("COLLECT", W / 2, H - 34);
      continue;
    }

    if (!body.isSensor) {
      const verts = body.vertices;
      ctx.beginPath();
      ctx.moveTo(verts[0].x, verts[0].y);
      for (let i = 1; i < verts.length; i++) {
        ctx.lineTo(verts[i].x, verts[i].y);
      }
      ctx.closePath();
      ctx.fillStyle = COLORS.wall;
      ctx.fill();
    }
  }

  drawLauncher();
}

function cleanOutOfBounds() {
  for (const ball of [...balls]) {
    if (
      ball.position.y > H + 100 ||
      ball.position.x < -100 ||
      ball.position.x > W + 100
    ) {
      removeBall(ball);
    }
  }
}

function loop(now) {
  const delta = Math.min(33, now - lastTime);
  lastTime = now;
  Engine.update(engine, delta);
  cleanOutOfBounds();
  draw();
  raf = requestAnimationFrame(loop);
}

canvas.addEventListener("pointerdown", (event) => {
  if (dropped) return;
  draggingLauncher = true;
  launcherX = clampLauncherX(pointerX(event));
  canvas.setPointerCapture?.(event.pointerId);
  event.preventDefault();
});

canvas.addEventListener("pointermove", (event) => {
  if (dropped || !draggingLauncher) return;
  launcherX = clampLauncherX(pointerX(event));
  event.preventDefault();
});

function stopDragging(event) {
  if (!draggingLauncher) return;
  draggingLauncher = false;
  canvas.releasePointerCapture?.(event.pointerId);
}

canvas.addEventListener("pointerup", stopDragging);
canvas.addEventListener("pointercancel", stopDragging);

dropBtn.addEventListener("click", dropInitialBalls);
restartBtn.addEventListener("click", setup);

window.addEventListener("resize", () => {
  setup();
});

setup();