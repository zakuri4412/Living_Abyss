using UnityEngine;
using UnityEngine.UI;
using LivingAbyss.Systems;

namespace LivingAbyss.UI
{
    // Organic symbiosis bar — no numbers, shape fills from bottom.  (GDD §10)
    // Breathes with cave pulse. Fades when the player stands still.
    [RequireComponent(typeof(CanvasGroup))]
    public class SymbiosisHUD : MonoBehaviour
    {
        // Shape dimensions in canvas pixels (canvas reference = 320×180).
        private const int SHAPE_W = 10;
        private const int SHAPE_H = 30;

        [Header("Images — wire via HUDSetupTool or manually")]
        [SerializeField] private Image _bg;
        [SerializeField] private Image _fill;

        [Header("Breathing")]
        [SerializeField, Range(0.05f, 2f)]  private float _breatheHz  = 0.25f; // T1 cave rhythm
        [SerializeField, Range(0f,  0.15f)] private float _breatheAmt = 0.06f;

        [Header("Idle Fade")]
        [SerializeField] private float _fadeDelay = 3f;
        [SerializeField] private float _fadeSpeed = 2f;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.28f;

        // GDD §9.3 zone palette — indices match SymbiosisZone enum order.
        private static readonly Color[] ZoneColors =
        {
            new Color(0.694f, 0.267f, 0.345f), // DANGER  — blood-rose
            new Color(0.549f, 0.376f, 0.125f), // LOW     — amber
            new Color(0.243f, 0.620f, 0.420f), // MID     — teal (baseline)
            new Color(0.369f, 0.792f, 0.627f), // HIGH    — bright teal
            new Color(0.635f, 0.910f, 0.812f), // HARMONY — pale cyan glow
        };

        private CanvasGroup     _group;
        private Rigidbody2D     _playerRb;
        private SymbiosisSystem _symb;
        private float           _displayValue;
        private float           _idleTimer;
        private Texture2D       _bgTex;
        private Texture2D       _fillTex;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            _symb         = SymbiosisSystem.Instance;
            _displayValue = _symb != null ? _symb.Value : 0.5f;

            var player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerRb = player.GetComponent<Rigidbody2D>();

            BuildSprites();
            SetZoneColor(_symb != null ? _symb.Zone : SymbiosisZone.Mid);

            EventBus.Subscribe<SymbZoneEnteredEvent>(OnZoneEntered);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SymbZoneEnteredEvent>(OnZoneEntered);
            if (_bgTex   != null) Destroy(_bgTex);
            if (_fillTex != null) Destroy(_fillTex);
        }

        private void Update()
        {
            if (_symb != null)
            {
                _displayValue = Mathf.Lerp(_displayValue, _symb.Value, 8f * Time.deltaTime);
                if (_fill != null)
                    _fill.fillAmount = _displayValue;
            }

            // Organic breathing — gentle sine-wave scale
            float breath = 1f + Mathf.Sin(Time.time * _breatheHz * Mathf.PI * 2f) * _breatheAmt;
            transform.localScale = new Vector3(breath, breath, 1f);

            // Idle fade: count idle time, fade toward _minAlpha, snap back when moving
            bool moving = _playerRb != null && _playerRb.linearVelocity.sqrMagnitude > 0.05f;
            if (moving)
            {
                _idleTimer = 0f;
                _group.alpha = Mathf.Lerp(_group.alpha, 1f, _fadeSpeed * 3f * Time.deltaTime);
            }
            else
            {
                _idleTimer += Time.deltaTime;
                float target = _idleTimer > _fadeDelay ? _minAlpha : 1f;
                _group.alpha = Mathf.Lerp(_group.alpha, target, _fadeSpeed * Time.deltaTime);
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnZoneEntered(SymbZoneEnteredEvent e) => SetZoneColor(e.Zone);

        private void SetZoneColor(SymbiosisZone zone)
        {
            if (_fill != null)
                _fill.color = ZoneColors[(int)zone];
        }

        // ── Sprite generation ─────────────────────────────────────────────────
        // Pixel-art ovals generated at runtime — no external sprites needed.

        private void BuildSprites()
        {
            _bgTex   = DrawOval(SHAPE_W, SHAPE_H, outline: true);
            _fillTex = DrawOval(SHAPE_W, SHAPE_H, outline: false);

            if (_bg   != null) _bg.sprite   = MakeSprite(_bgTex);
            if (_fill != null) _fill.sprite = MakeSprite(_fillTex);
        }

        // outline=true  → 1-px dim border (background underlay)
        // outline=false → solid white fill (tinted by Image.color)
        private static Texture2D DrawOval(int w, int h, bool outline)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };

            var pixels = new Color[w * h]; // default: transparent

            float cx  = (w - 1) * 0.5f;
            float cy  = (h - 1) * 0.5f;
            float rx  = w * 0.5f;
            float ry  = h * 0.5f;
            float irx = Mathf.Max(0f, rx - 1.5f);
            float iry = Mathf.Max(0f, ry - 1.5f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                if (dx * dx + dy * dy > 1f) continue; // outside outer ellipse

                if (outline)
                {
                    float idx = irx > 0f ? (x - cx) / irx : float.MaxValue;
                    float idy = iry > 0f ? (y - cy) / iry : float.MaxValue;
                    if (idx * idx + idy * idy <= 1f) continue; // inside inner ellipse → skip
                    pixels[y * w + x] = new Color(0.75f, 0.72f, 0.78f, 0.70f); // soft lavender outline
                }
                else
                {
                    pixels[y * w + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Sprite MakeSprite(Texture2D tex)
        {
            // PPU=1: the canvas scaler handles 4× upscaling from game to screen resolution.
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
        }
    }
}
