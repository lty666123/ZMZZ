namespace ZMZZ.Combat
{
    /// <summary>
    /// 受击方的「防守」能力。目前只有玩家实现它（格挡 / 弹反 / 无敌帧）。
    /// 敌人不实现，于是所有攻击对敌人来说都是直接命中。
    /// </summary>
    public interface IDamageGuard
    {
        /// <summary>处于无敌状态（闪避无敌帧、受击后的无敌帧）。</summary>
        bool IsInvulnerable { get; }

        /// <summary>只做判定、不产生副作用。用于决定这次攻击是格挡、弹反还是打空。</summary>
        BlockOutcome EvaluateBlock(ref DamageInfo info);

        /// <summary>判定结果出来后的副作用（扣体力、进硬直、播特效等）。</summary>
        void OnBlockResolved(ref DamageInfo info, BlockOutcome outcome);
    }

    /// <summary>可以被「弹反 / 破防」打断当前动作的对象（Boss 的连招、玩家的攻击）。</summary>
    public interface IInterruptible
    {
        void Interrupt(int frames, string reason);
    }
}
