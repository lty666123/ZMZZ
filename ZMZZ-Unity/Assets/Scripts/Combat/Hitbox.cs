using System;
using System.Collections.Generic;
using UnityEngine;
using ZMZZ.Core;

namespace ZMZZ.Combat
{
    /// <summary>
    /// 攻击判定盒。用物理查询而不是触发器，好处是时序确定、可用 Gizmos 直接画出来调。
    /// 何时开始 / 结束判定由 AttackRunner 按帧数据驱动。
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        public Team team = Team.Player;
        public Transform owner;
        public LayerMask targetMask;
        public float facing = 1f;

        [Tooltip("勾选后一次挥击可以多次命中（旋风类）；默认一次挥击每个目标只吃一次")]
        public bool multiHit = false;
        public int reHitIntervalFrames = 15;

        [Header("原型期可视化")]
        public bool showVisual = true;
        public SpriteRenderer visual;
        public Color visualColor = new Color(1f, 1f, 1f, 0.30f);
        public Color unblockableVisualColor = new Color(1f, 0.35f, 0.25f, 0.42f);

        public AttackProfile Profile { get { return _profile; } }
        public bool IsActive { get { return _active; } }
        public int ActiveFrame { get { return _frame; } }

        public event Action<Hurtbox, BlockOutcome> HitLanded;

        AttackProfile _profile;
        bool _active;
        int _frame;
        int _lastMultiHitFrame = -9999;
        readonly HashSet<Hurtbox> _alreadyHit = new HashSet<Hurtbox>();

        void Awake()
        {
            if (owner == null) owner = transform.root;
        }

        public void Begin(AttackProfile profile, float facingDirection)
        {
            _profile = profile;
            facing = facingDirection >= 0f ? 1f : -1f;
            _active = true;
            _frame = 0;
            _alreadyHit.Clear();
            _lastMultiHitFrame = -9999;
            UpdateVisual(true);
        }

        public void End()
        {
            _active = false;
            UpdateVisual(false);
        }

        void FixedUpdate()
        {
            if (!_active || _profile == null) return;
            _frame++;

            Vector2 size = new Vector2(_profile.boxWidth, _profile.boxHeight);
            Vector2 center = BoxCenter;
            var overlaps = Physics2D.OverlapBoxAll(center, size, 0f, targetMask);

            for (int i = 0; i < overlaps.Length; i++)
            {
                var target = overlaps[i].GetComponentInParent<Hurtbox>();
                if (target == null) continue;
                if (target.team == team) continue;

                if (!multiHit)
                {
                    if (_alreadyHit.Contains(target)) continue;
                }
                else if (_frame - _lastMultiHitFrame < reHitIntervalFrames)
                {
                    continue;
                }

                // 命中点取判定盒与受击盒的水平中点、纵向接近目标中心
                Vector2 hitPoint = new Vector2(
                    Mathf.Lerp(center.x, target.Center.x, 0.5f),
                    Mathf.Lerp(center.y, target.Center.y, 0.5f));

                var info = BuildDamageInfo(hitPoint, target);
                var result = DamageResolver.Resolve(ref info, target);

                if (result.Consumed)
                {
                    _alreadyHit.Add(target);
                    _lastMultiHitFrame = _frame;
                    HitLanded?.Invoke(target, result.Outcome);
                }
            }
        }

        DamageInfo BuildDamageInfo(Vector2 hitPoint, Hurtbox target)
        {
            var info = new DamageInfo();
            info.attackerTeam = team;
            info.attacker = owner != null ? owner : transform;
            info.attackerHurtbox = owner != null ? owner.GetComponentInChildren<Hurtbox>() : null;
            info.hitPoint = hitPoint;
            info.damage = _profile.damage;
            info.staggerDamage = _profile.staggerDamage;
            info.knockback = _profile.knockback;
            info.hitStopFrames = _profile.hitStopFrames;
            info.blockable = _profile.blockable;
            info.sourceName = _profile.label;
            return info;
        }

        public Vector2 BoxCenter
        {
            get
            {
                if (_profile == null) return transform.position;
                Vector3 origin = owner != null ? owner.position : transform.position;
                return new Vector2(
                    origin.x + _profile.boxOffsetX * facing,
                    origin.y + _profile.boxOffsetY);
            }
        }

        public Vector2 BoxSize
        {
            get { return _profile != null ? new Vector2(_profile.boxWidth, _profile.boxHeight) : Vector2.one; }
        }

        void UpdateVisual(bool on)
        {
            if (!showVisual || visual == null) return;
            visual.enabled = on;
            if (!on || _profile == null) return;

            visual.transform.position = BoxCenter;
            visual.transform.localScale = new Vector3(_profile.boxWidth, _profile.boxHeight, 1f);
            visual.color = _profile.blockable ? visualColor : unblockableVisualColor;
        }

        void OnDrawGizmosSelected()
        {
            var profile = _profile;
            if (profile == null) return;
            Gizmos.color = profile.blockable ? new Color(0f, 1f, 1f, 0.25f) : new Color(1f, 0.3f, 0.2f, 0.3f);
            Gizmos.DrawCube(BoxCenter, BoxSize);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(BoxCenter, BoxSize);
        }
    }
}
