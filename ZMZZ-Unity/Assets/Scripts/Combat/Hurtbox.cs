using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>
    /// 受击盒：所有伤害进入角色的唯一入口。挂在角色的子物体上，带一个 Collider2D。
    /// 它不处理规则，只负责把「我是谁、我有什么」交给 DamageResolver。
    /// </summary>
    public class Hurtbox : MonoBehaviour
    {
        public Team team = Team.Enemy;

        [Tooltip("角色根节点")]
        public Transform owner;

        [Tooltip("被击退的倍率：小怪 1，Boss 这种重甲单位调小")]
        public float knockbackScale = 1f;

        public Health health;
        public Stagger stagger;          // 可以为空：小怪没有韧性条
        public Rigidbody2D body;

        [HideInInspector] public IDamageGuard guard;
        [HideInInspector] public IInterruptible interruptible;

        void Awake()
        {
            Resolve();
        }

        public void Resolve()
        {
            if (owner == null) owner = transform.root;
            if (health == null) health = GetComponentInParent<Health>();
            if (stagger == null) stagger = GetComponentInParent<Stagger>();
            if (body == null) body = GetComponentInParent<Rigidbody2D>();
            guard = FindInterface<IDamageGuard>();
            interruptible = FindInterface<IInterruptible>();
        }

        T FindInterface<T>() where T : class
        {
            var behaviours = GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                var found = behaviours[i] as T;
                if (found != null) return found;
            }
            return null;
        }

        public Vector2 Center
        {
            get { return transform.position; }
        }

        public float DamageMultiplier
        {
            get { return stagger != null ? stagger.DamageMultiplier : 1f; }
        }

        public bool IsDead
        {
            get { return health == null || health.IsDead; }
        }
    }
}
