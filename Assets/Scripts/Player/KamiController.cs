using System;
using System.Collections.Generic;
using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// Kami: moves with the current shape's numbers, folds into belt shapes (1 crease each), and follows
    /// the paper rules. Water sinks every shape except the boat (floats) and the fish (swims); wind lifts
    /// the crane; ink stains block folding for 3 s; fire, spikes and ink pits tear.
    /// The transform sits at Kami's feet; the collider is resized per shape from the bottom up.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [RequireComponent(typeof(CreaseCounter), typeof(FoldBelt))]
    public class KamiController : MonoBehaviour
    {
        [Header("Feel")]
        [SerializeField] float gravity = 32f;
        [SerializeField] float groundAcceleration = 70f;
        [SerializeField] float airAcceleration = 40f;
        [SerializeField] float maxFallSpeed = 18f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBufferTime = 0.12f;
        [SerializeField, Range(0f, 1f)] float jumpCut = 0.5f;
        [SerializeField] float stepUpHeight = 0.3f;
        [Tooltip("Floating shapes can beach themselves onto banks this high.")]
        [SerializeField] float floatStepUpHeight = 0.8f;

        [Header("Paper rules")]
        [SerializeField] float stainSeconds = 3f;
        [Tooltip("How long a non-floating shape survives in water. Fold into the boat or fish in time!")]
        [SerializeField] float sinkSeconds = 0.7f;
        [Tooltip("Part of a floating shape's height that sits under the water line.")]
        [SerializeField, Range(0.1f, 0.6f)] float draftFraction = 0.35f;

        [Header("Abilities")]
        [SerializeField] Sprite paperStarSprite;
        [SerializeField] Material paperStarMaterial;
        [SerializeField] float starSpeed = 14f;
        [SerializeField] float starRange = 8f;
        [SerializeField] Sprite gustSprite;
        [SerializeField] float gustRange = 4f;
        [SerializeField] float gustForce = 9f;
        [SerializeField] float dashDistance = 10f;
        [SerializeField] float dashSpeed = 12f;

        [Header("Parts")]
        [SerializeField] KamiVisual visual;

        public ShapeData Shape { get; private set; }
        public CreaseCounter Creases { get; private set; }
        public FoldBelt Belt { get; private set; }
        public LevelData Level { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsFloating { get; private set; }
        public bool IsTorn { get; private set; }
        public int Facing { get; private set; } = 1;
        public float StainTimeLeft { get; private set; }
        public float StainSeconds => stainSeconds;
        public Vector2 Velocity => body.linearVelocity;
        public Vector2 Center => body.position + box.offset;

        /// <summary>Movement and folding are ignored while locked (level complete, belt editing...).</summary>
        public bool Locked { get; set; }
        /// <summary>The pressing stone Kami is standing at, if any (Tab edits the belt there).</summary>
        public PressingStone StoneHere { get; set; }
        public bool BeltEditing { get; private set; }

        public event Action<ShapeData> ShapeChanged;
        public event Action<string> Torn;
        public event Action<string> Notice;
        public event Action BeltEditingChanged;

        Rigidbody2D body;
        BoxCollider2D box;
        KamiInput input;
        ContactFilter2D solidFilter;
        ContactFilter2D anyFilter;
        readonly List<RaycastHit2D> hits = new();
        readonly List<Collider2D> overlaps = new();
        readonly List<WaterVolume> waters = new();
        readonly List<WindColumn> winds = new();

        float moveX;
        Vector2 moveRaw;
        bool jumpHeld, jumping;
        float coyoteTimer, jumpBufferTimer, abilityTimer, dashTimeLeft, sinkTimer, swimTimer;
        bool warnedAir;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            Creases = GetComponent<CreaseCounter>();
            Belt = GetComponent<FoldBelt>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            solidFilter = new ContactFilter2D { useTriggers = false };
            anyFilter = new ContactFilter2D { useTriggers = true };
            Creases.Torn += () => Tear("Too many creases. The paper tore!");
            if (visual == null)
                visual = GetComponentInChildren<KamiVisual>();
        }

        public void Setup(LevelData level, KamiInput kamiInput)
        {
            Level = level;
            input = kamiInput;
            Creases.Setup(level != null ? level.creaseLimit : 8);
            if (level != null)
                Belt.Setup(level.startingBelt);
            SetShape(Belt[0]);
        }

        void Update()
        {
            if (input == null || IsTorn || Shape == null)
                return;

            StainTimeLeft = Mathf.Max(0f, StainTimeLeft - Time.deltaTime);
            abilityTimer -= Time.deltaTime;

            moveRaw = Locked || BeltEditing ? Vector2.zero : input.MoveValue;
            moveX = Mathf.Abs(moveRaw.x) > 0.25f ? Mathf.Sign(moveRaw.x) : 0f;
            if (moveX != 0f && dashTimeLeft <= 0f)
                Facing = (int)moveX;

            if (Locked)
                return;

            if (input.SwapBelt.WasPressedThisFrame())
                SetBeltEditing(!BeltEditing);
            if (BeltEditing && StoneHere == null)
                SetBeltEditing(false);

            for (int i = 0; i < FoldBelt.Size; i++)
            {
                if (!input.FoldSlots[i].WasPressedThisFrame())
                    continue;
                if (BeltEditing)
                    Belt.Cycle(i, Level != null ? Level.swapPool : null);
                else
                    FoldTo(i);
            }

            if (BeltEditing)
                return;
            if (input.Jump.WasPressedThisFrame())
                jumpBufferTimer = jumpBufferTime;
            jumpHeld = input.Jump.IsPressed();
            if (input.Ability.WasPressedThisFrame())
                UseAbility();
        }

        void FixedUpdate()
        {
            if (IsTorn || Shape == null)
                return;

            float dt = Time.fixedDeltaTime;
            UpdateGrounded();
            var v = body.linearVelocity;

            if (dashTimeLeft > 0f)
            {
                dashTimeLeft -= dt;
                body.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
                return;
            }

            float bottom = body.position.y;
            float height = box.size.y;
            var water = CurrentWater();
            float surface = water != null ? water.SurfaceY : float.NegativeInfinity;
            bool wet = water != null && bottom + height * 0.5f < surface;
            IsFloating = water != null && Shape.floats && bottom < surface;

            float speed = (IsFloating || (wet && Shape.swims)) ? Shape.waterSpeed : Shape.landSpeed;
            float accel = IsGrounded || IsFloating ? groundAcceleration : airAcceleration;
            v.x = Mathf.MoveTowards(v.x, moveX * speed, accel * dt);

            if (IsFloating)
            {
                // Spring the boat to its water line.
                float target = surface - height * draftFraction;
                v.y += ((target - bottom) * 60f - v.y * 10f) * dt;
                sinkTimer = 0f;
            }
            else if (wet && Shape.swims)
            {
                float swim = jumpHeld || moveRaw.y > 0.3f ? 1f : moveRaw.y < -0.3f ? -1f : -0.15f;
                v.y = Mathf.MoveTowards(v.y, swim * Shape.waterSpeed * 0.7f, 20f * dt);
                if (bottom + height > surface + 0.1f && v.y > 0f)
                    v.y = 0f;
                UpdateSwimTimer(bottom + height < surface, dt);
                sinkTimer = 0f;
            }
            else if (wet)
            {
                // Soaked: everything else sinks.
                sinkTimer += dt;
                v.x = Mathf.MoveTowards(v.x, 0f, 20f * dt);
                v.y = Mathf.MoveTowards(v.y, Shape.heavy ? -4f : -1.2f, 30f * dt);
                if (sinkTimer >= sinkSeconds)
                {
                    Tear(Shape.heavy ? "The box sank like a stone." : "Soaked through! Only the boat and the fish stay afloat.");
                    return;
                }
            }
            else
            {
                sinkTimer = 0f;
                swimTimer = 0f;
                warnedAir = false;
                v.y -= gravity * dt;
                var wind = winds.Count > 0 ? winds[winds.Count - 1] : null;
                if (wind != null && (Shape.ridesWind || Shape.blownByWind))
                {
                    v.y = Mathf.MoveTowards(v.y, Shape.blownByWind ? wind.LiftSpeed * 0.6f : wind.LiftSpeed, 60f * dt);
                    if (Shape.blownByWind)
                        v.x += wind.SidePush * dt;
                }
                else if (Shape.glideFallSpeed > 0f && v.y < -Shape.glideFallSpeed)
                {
                    v.y = -Shape.glideFallSpeed;
                }
                v.y = Mathf.Max(v.y, -maxFallSpeed);
            }

            // Jump with coyote time and a short input buffer; release early for a lower jump.
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && Shape.jumpHeight > 0f && !wet)
            {
                v.y = Mathf.Sqrt(2f * gravity * Shape.jumpHeight);
                jumping = true;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                if (visual != null)
                    visual.PlayJump();
            }
            jumpBufferTimer -= dt;
            if (jumping && !jumpHeld && v.y > 0f)
            {
                v.y *= jumpCut;
                jumping = false;
            }
            if (v.y <= 0f)
                jumping = false;

            if (moveX != 0f && (IsGrounded || IsFloating))
                TryStepUp(moveX, IsFloating ? floatStepUpHeight : stepUpHeight);

            body.linearVelocity = v;
        }

        // ------------------------------------------------------------------ folding

        public void FoldTo(int slot)
        {
            if (IsTorn || Locked)
                return;
            var target = Belt[slot];
            if (target == null)
            {
                Notice?.Invoke($"Slot {slot + 1} is empty.");
                return;
            }
            if (target == Shape)
                return;
            if (StainTimeLeft > 0f)
            {
                Notice?.Invoke("Ink stain! You can't fold for a moment.");
                if (visual != null)
                    visual.Shake();
                return;
            }
            var size = target.colliderSize;
            if (!BoxIsFree(body.position + new Vector2(0f, size.y * 0.5f + 0.03f), size * 0.94f))
            {
                Notice?.Invoke("No room to fold here.");
                if (visual != null)
                    visual.Shake();
                return;
            }

            SetShape(target);
            if (visual != null)
                visual.PlayFold();
            if (Shape.ability == ShapeAbility.PlaneDash)
                StartDash();
            Creases.AddCrease();
        }

        void SetShape(ShapeData shape)
        {
            if (shape == null)
                return;
            Shape = shape;
            box.size = shape.colliderSize;
            box.offset = new Vector2(0f, shape.colliderSize.y * 0.5f);
            body.mass = shape.heavy ? 5f : 1f;
            swimTimer = 0f;
            if (visual != null)
                visual.SetShape(shape);
            ShapeChanged?.Invoke(shape);
        }

        void SetBeltEditing(bool editing)
        {
            if (editing && StoneHere == null)
            {
                Notice?.Invoke("Stand at a pressing stone to change your belt.");
                return;
            }
            if (BeltEditing == editing)
                return;
            BeltEditing = editing;
            BeltEditingChanged?.Invoke();
        }

        // ------------------------------------------------------------------ abilities

        void UseAbility()
        {
            if (abilityTimer > 0f)
                return;
            switch (Shape.ability)
            {
                case ShapeAbility.PaperStar:
                    var origin = body.position + new Vector2(Facing * 0.45f, box.size.y * 0.62f);
                    PaperStar.Launch(paperStarSprite, paperStarMaterial, origin, Facing, starSpeed, starRange, box);
                    break;
                case ShapeAbility.FanGust:
                    Gust();
                    break;
                case ShapeAbility.PlaneDash:
                    StartDash();
                    break;
                default:
                    return;
            }
            abilityTimer = Shape.abilityCooldown;
            if (visual != null)
                visual.PlayAbility();
        }

        void Gust()
        {
            var centre = body.position + new Vector2(Facing * gustRange * 0.5f, box.size.y * 0.5f);
            overlaps.Clear();
            Physics2D.OverlapBox(centre, new Vector2(gustRange, 1.6f), 0f, anyFilter, overlaps);
            foreach (var other in overlaps)
            {
                if (other == box)
                    continue;
                if (other.GetComponentInParent<InkBlot>() is { } blot)
                    blot.Push(Facing);
                else if (other.GetComponentInParent<Hazard>() is { Extinguishable: true } fire)
                    fire.Extinguish();
                else if (other.attachedRigidbody is { bodyType: RigidbodyType2D.Dynamic } rb && rb != body)
                    rb.AddForce(new Vector2(Facing * gustForce, 1.5f), ForceMode2D.Impulse);
            }
            if (gustSprite != null)
                OneShotFx.Spawn(gustSprite, paperStarMaterial, centre, 0.35f, 0.7f, 1.05f, flipX: Facing < 0,
                                velocity: new Vector2(Facing * 4f, 0f));
        }

        void StartDash()
        {
            dashTimeLeft = dashDistance / Mathf.Max(dashSpeed, 0.1f);
        }

        // ------------------------------------------------------------------ paper rules

        /// <summary>An Ink Blot touched Kami. Returns false if the blot bounced off (box form).</summary>
        public bool TryStain()
        {
            if (IsTorn)
                return true;
            if (Shape.blotsBounceOff)
            {
                if (visual != null)
                    visual.Bump();
                return false;
            }
            if (StainTimeLeft <= 0f)
                Notice?.Invoke("Ink stain! No folding for 3 s.");
            StainTimeLeft = stainSeconds;
            return true;
        }

        public void Tear(string reason)
        {
            if (IsTorn)
                return;
            IsTorn = true;
            BeltEditing = false;
            dashTimeLeft = 0f;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            if (visual != null)
                visual.PlayTear();
            Torn?.Invoke(reason);
        }

        public void Respawn(Vector2 position)
        {
            waters.Clear();
            winds.Clear();
            StoneHere = null;
            transform.position = position;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.simulated = true;
            IsTorn = false;
            StainTimeLeft = 0f;
            sinkTimer = swimTimer = dashTimeLeft = 0f;
            jumpBufferTimer = 0f;
            SetShape(Belt[0]);
            Creases.Smooth();
            if (visual != null)
                visual.ResetLook();
        }

        void UpdateSwimTimer(bool underwater, float dt)
        {
            if (!underwater)
            {
                swimTimer = 0f;
                warnedAir = false;
                return;
            }
            swimTimer += dt;
            if (!warnedAir && swimTimer > Shape.swimSeconds - 2f)
            {
                warnedAir = true;
                Notice?.Invoke("The fish is getting soggy. Surface!");
            }
            if (swimTimer >= Shape.swimSeconds)
                Tear("Too long under water. The fish went soggy.");
        }

        // ------------------------------------------------------------------ physics helpers

        void UpdateGrounded()
        {
            IsGrounded = false;
            hits.Clear();
            box.Cast(Vector2.down, solidFilter, hits, 0.06f);
            foreach (var hit in hits)
            {
                if (hit.normal.y > 0.6f)
                {
                    IsGrounded = true;
                    return;
                }
            }
        }

        void TryStepUp(float direction, float maxHeight)
        {
            hits.Clear();
            box.Cast(new Vector2(direction, 0f), solidFilter, hits, 0.05f);
            bool blocked = false;
            foreach (var hit in hits)
                blocked |= Mathf.Abs(hit.normal.x) > 0.7f;
            if (!blocked)
                return;

            for (float h = 0.1f; h <= maxHeight + 0.001f; h += 0.1f)
            {
                var centre = body.position + box.offset + new Vector2(direction * 0.08f, h + 0.02f);
                if (BoxIsFree(centre, box.size * 0.96f))
                {
                    body.position += new Vector2(0f, h + 0.02f);
                    return;
                }
            }
        }

        bool BoxIsFree(Vector2 centre, Vector2 size)
        {
            overlaps.Clear();
            Physics2D.OverlapBox(centre, size, 0f, solidFilter, overlaps);
            foreach (var other in overlaps)
                if (other != box)
                    return false;
            return true;
        }

        WaterVolume CurrentWater()
        {
            WaterVolume best = null;
            foreach (var w in waters)
                if (w != null && (best == null || w.SurfaceY > best.SurfaceY))
                    best = w;
            return best;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out WaterVolume water) && !waters.Contains(water))
                waters.Add(water);
            if (other.TryGetComponent(out WindColumn wind) && !winds.Contains(wind))
                winds.Add(wind);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out WaterVolume water))
                waters.Remove(water);
            if (other.TryGetComponent(out WindColumn wind))
                winds.Remove(wind);
        }
    }
}
