const { Engine, World, Bodies, Body, Events } = Matter;

const canvas = document.getElementById("game");
const wrap = document.getElementById("gameWrap");
const dropBtn = document.getElementById("dropBtn");
const restartBtn = document.getElementById("restartBtn");
const ballCountEl = document.getElementById("ballCount");
const collectedEl = document.getElementById("collected");
const statusEl = document.getElementById("status");

let engine, ctx;
let W = 420, H = 720;
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
  bg1: "#161e33", bg2: "#0a0e18", wall: "#3a4154", ball: "#ffffff",
  outline: "#111826", gate2: "#f4aa24", gate3: "#7a5cff", gate5: "#69b929",
  jump: "#24b8e7", bucket: "#222838", pin: "#65708a", text: "#ffffff"
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
  W = rect.width; H = rect.height;
  launcherX = clampLauncherX(launcherX || W / 2);
}

function clampLauncherX(x) {
  const half = ((INITIAL_BALLS - 1) * LAUNCH_SPACING) / 2 + BALL_R + 12;
  return Math.max(half + 18, Math.min(W - half - 18, x));
}

function pointerX(e) { return e.clientX - canvas.getBoundingClientRect().left; }

function addStaticRect(x, y, w, h, angle = 0, label = "wall") {
  const b = Bodies.rectangle(x, y, w, h, { isStatic: true, angle, label });
  World.add(engine.world, b); return b;
}

function addSensorRect(x, y, w, h, label, data = {}) {
  const b = Bodies.rectangle(x, y, w, h, { isStatic: true, isSensor: true, label });
  Object.assign(b, data); World.add(engine.world, b); return b;
}

function createBall(x, y, vx = 0, vy = 0) {
  if (balls.size >= MAX_BALLS) return null;
  const ball = Bodies.circle(x, y, BALL_R, {
    restitution: 0.35, friction: 0.005, frictionAir: 0.0015, density: 0.0018, label: "ball"
  });
  ball.plugin = { gateCooldown: 0, jumpCooldown: 0 };
  Body.setVelocity(ball, { x: vx, y: vy });
  balls.add(ball); World.add(engine.world, ball); updateHud(); return ball;
}

function removeBall(ball) {
  if (!balls.has(ball)) return;
  balls.delete(ball); World.remove(engine.world, ball); updateHud();
}

function createLevel() {
  const wall = 18;
  addStaticRect(wall / 2, H / 2, wall, H);
  addStaticRect(W - wall / 2, H / 2, wall, H);

  // Row 1: a clean choice. Nothing blocks the balls above or below the gates.
  addGate(W * 0.25, H * 0.30, W * 0.38, 38, 2);
  addGate(W * 0.75, H * 0.30, W * 0.38, 38, 3);

  // Small Plinko section: enough variation without trapping balls.
  const pins = [
    [0.30, 0.42], [0.50, 0.42], [0.70, 0.42],
    [0.40, 0.49], [0.60, 0.49]
  ];
  for (const [px, py] of pins) {
    World.add(engine.world, Bodies.circle(W * px, H * py, 8, { isStatic: true, label: "pin" }));
  }

  // Row 2: jump or multiplier. Both are completely pass-through sensors.
  addJumpPad(W * 0.25, H * 0.61, W * 0.38, 38, 8.5);
  addGate(W * 0.75, H * 0.61, W * 0.38, 38, 5);

  // Two tiny deflectors only; wide central opening to guarantee progression.
  addStaticRect(W * 0.18, H * 0.75, W * 0.22, 10, 0.20);
  addStaticRect(W * 0.82, H * 0.75, W * 0.22, 10, -0.20);

  // Wide collector so balls cannot get stranded at the bottom.
  const collector = addSensorRect(W / 2, H - 30, W - 46, 54, "collector");
  collector.renderData = { x: W / 2, y: H - 30, w: W - 46, h: 54 };
}

function addGate(x, y, w, h, multiplier) {
  const sensor = addSensorRect(x, y, w, h, "gate", { multiplier });
  sensor.renderData = { x, y, w, h, multiplier };
}

function addJumpPad(x, y, w, h, power) {
  const sensor = addSensorRect(x, y, w, h, "jump", { jumpPower: power });
  sensor.renderData = { x, y, w, h, power };
}

function onCollision(event) {
  for (const pair of event.pairs) {
    const a = pair.bodyA, b = pair.bodyB;
    const ball = a.label === "ball" ? a : b.label === "ball" ? b : null;
    const other = ball === a ? b : a;
    if (!ball) continue;

    if (other.label === "gate") multiplyBall(ball, other.multiplier);

    if (other.label === "jump") {
      const now = performance.now();
      if (!ball.plugin.jumpCooldown || now > ball.plugin.jumpCooldown) {
        Body.setVelocity(ball, { x: ball.velocity.x, y: -Math.abs(other.jumpPower || 8.5) });
        ball.plugin.jumpCooldown = now + 700;
      }
    }

    if (other.label === "collector") {
      collected++;
      removeBall(ball);
      collectedEl.textContent = collected;
    }
  }
}

function multiplyBall(ball, multiplier) {
  const now = performance.now();
  if (!ball.plugin) ball.plugin = {};
  if (ball.plugin.gateCooldown && now < ball.plugin.gateCooldown) return;
  const x = ball.position.x, y = ball.position.y;
  const vx = ball.velocity.x, vy = Math.max(1.5, ball.velocity.y);
  removeBall(ball);
  const count = Math.min(multiplier, MAX_BALLS - balls.size);
  for (let i = 0; i < count; i++) {
    const spread = (i - (count - 1) / 2) * 4.5;
    const b = createBall(x + spread, y + 10, vx + spread * 0.045, vy + Math.random() * 0.25);
    if (b) b.plugin.gateCooldown = now + 350;
  }
}

function setup() {
  if (raf) cancelAnimationFrame(raf);
  resizeCanvas(); launcherX = W / 2; draggingLauncher = false;
  engine = Engine.create({ gravity: { x: 0, y: 1, scale: 0.00135 } });
  balls = new Set(); collected = 0; dropped = false;
  collectedEl.textContent = "0";
  statusEl.textContent = "Drag balls left or right";
  dropBtn.disabled = false; dropBtn.style.display = "block"; dropBtn.textContent = "DROP 5 BALLS";
  createLevel(); Events.on(engine, "collisionStart", onCollision);
  lastTime = performance.now(); loop(lastTime); updateHud();
}

function dropInitialBalls() {
  if (dropped) return;
  dropped = true; draggingLauncher = false; dropBtn.disabled = true; dropBtn.style.display = "none";
  statusEl.textContent = "Running";
  for (let i = 0; i < INITIAL_BALLS; i++) {
    createBall(launcherX + (i - 2) * LAUNCH_SPACING, LAUNCH_Y, (i - 2) * 0.05, 0);
  }
}

function updateHud() {
  ballCountEl.textContent = dropped ? balls.size : INITIAL_BALLS;
  if (dropped && balls.size === 0) statusEl.textContent = `Finished — ${collected} collected`;
}

function drawRoundedRect(x, y, w, h, r, fill) {
  ctx.beginPath(); ctx.roundRect(x, y, w, h, r); ctx.fillStyle = fill; ctx.fill();
}

function drawLauncher() {
  if (dropped) return;
  ctx.save(); ctx.setLineDash([5, 7]); ctx.strokeStyle = "rgba(255,255,255,.16)"; ctx.lineWidth = 1;
  ctx.beginPath(); ctx.moveTo(launcherX, LAUNCH_Y + 15); ctx.lineTo(launcherX, H * 0.25); ctx.stroke(); ctx.setLineDash([]);
  for (let i = 0; i < INITIAL_BALLS; i++) {
    const x = launcherX + (i - 2) * LAUNCH_SPACING;
    ctx.beginPath(); ctx.arc(x, LAUNCH_Y, BALL_R, 0, Math.PI * 2); ctx.fillStyle = COLORS.ball; ctx.fill();
    ctx.lineWidth = 2; ctx.strokeStyle = COLORS.outline; ctx.stroke();
  }
  ctx.fillStyle = "rgba(255,255,255,.62)"; ctx.font = "800 11px Arial"; ctx.textAlign = "center";
  ctx.fillText("↔ DRAG TO AIM", launcherX, LAUNCH_Y + 31); ctx.restore();
}

function draw() {
  const g = ctx.createLinearGradient(0, 0, 0, H); g.addColorStop(0, COLORS.bg1); g.addColorStop(1, COLORS.bg2);
  ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);

  for (const body of engine.world.bodies) {
    if (body.label === "ball") {
      ctx.beginPath(); ctx.arc(body.position.x, body.position.y, BALL_R, 0, Math.PI * 2);
      ctx.fillStyle = COLORS.ball; ctx.fill(); ctx.lineWidth = 2; ctx.strokeStyle = COLORS.outline; ctx.stroke(); continue;
    }
    if (body.label === "pin") {
      ctx.beginPath(); ctx.arc(body.position.x, body.position.y, 8, 0, Math.PI * 2); ctx.fillStyle = COLORS.pin; ctx.fill(); continue;
    }
    if (body.label === "gate") {
      const d = body.renderData;
      const c = d.multiplier === 5 ? COLORS.gate5 : d.multiplier === 3 ? COLORS.gate3 : COLORS.gate2;
      drawRoundedRect(d.x - d.w/2, d.y - d.h/2, d.w, d.h, 8, c);
      ctx.fillStyle = COLORS.text; ctx.font = "900 28px Arial"; ctx.textAlign = "center"; ctx.textBaseline = "middle";
      ctx.fillText("×" + d.multiplier, d.x, d.y + 1); continue;
    }
    if (body.label === "jump") {
      const d = body.renderData; drawRoundedRect(d.x-d.w/2, d.y-d.h/2, d.w, d.h, 8, COLORS.jump);
      ctx.fillStyle = COLORS.text; ctx.font = "900 28px Arial"; ctx.textAlign = "center"; ctx.textBaseline = "middle";
      ctx.fillText("⇈", d.x, d.y); continue;
    }
    if (body.label === "collector") {
      const d = body.renderData; drawRoundedRect(d.x-d.w/2, d.y-d.h/2, d.w, d.h, 10, COLORS.bucket);
      ctx.fillStyle = "#b9c4df"; ctx.font = "800 14px Arial"; ctx.textAlign = "center"; ctx.textBaseline = "middle";
      ctx.fillText("COLLECT", d.x, d.y); continue;
    }
    if (!body.isSensor) {
      const v = body.vertices; ctx.beginPath(); ctx.moveTo(v[0].x,v[0].y);
      for (let i=1;i<v.length;i++) ctx.lineTo(v[i].x,v[i].y);
      ctx.closePath(); ctx.fillStyle = COLORS.wall; ctx.fill();
    }
  }
  drawLauncher();
}

function cleanOutOfBounds() {
  for (const ball of [...balls]) {
    if (ball.position.y > H + 100 || ball.position.x < -100 || ball.position.x > W + 100) removeBall(ball);
  }
}

function loop(now) {
  const delta = Math.min(33, now-lastTime); lastTime = now; Engine.update(engine, delta); cleanOutOfBounds(); draw();
  raf = requestAnimationFrame(loop);
}

canvas.addEventListener("pointerdown", e => {
  if (dropped) return; draggingLauncher = true; launcherX = clampLauncherX(pointerX(e));
  canvas.setPointerCapture?.(e.pointerId); e.preventDefault();
});
canvas.addEventListener("pointermove", e => {
  if (dropped || !draggingLauncher) return; launcherX = clampLauncherX(pointerX(e)); e.preventDefault();
});
function stopDragging(e) { if (!draggingLauncher) return; draggingLauncher=false; canvas.releasePointerCapture?.(e.pointerId); }
canvas.addEventListener("pointerup", stopDragging); canvas.addEventListener("pointercancel", stopDragging);
dropBtn.addEventListener("click", dropInitialBalls); restartBtn.addEventListener("click", setup);
window.addEventListener("resize", setup);
setup();