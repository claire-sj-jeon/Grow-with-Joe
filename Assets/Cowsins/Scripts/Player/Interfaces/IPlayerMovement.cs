namespace cowsins2D
{
    using UnityEngine;
    public interface IPlayerMovement
    {
        PlayerMovementEvents PlayerMovementEvents { get; set; }
        Transform Graphics { get; }
        bool isGliding { get; }
        bool ladderAvailable { get; }
        Ladder CurrentLadder { get; }
        bool IsOnLadderCooldown { get; }
        bool facingRight { get; }
        float currentSpeed { get; }
        float RunSpeed { get; }
        bool IsGrounded { get; }
        bool IsFalling { get; }
        float stamina { get; }
        bool UsesStamina { get; }
        float MaxStamina { get; }
        bool isJumping { get; }
        bool isWallJumping { get; }
        bool isWallSliding { get; }
        bool isCrouching { get; }
        bool isDashing { get; }
        bool InvincibleWhileDashing { get; }
        int AmountOfDashes { get; }
        float LastOnGroundTime { get; }
        bool IsScriptedMove { get; }
        void OnJumpInput();
        void OnJumpUpInput();
        bool CanDash();

        // 컷신에서 조작 없이 캐릭터를 평소 이동 로직 그대로 걷게 하기 위한 강제 입력.
        void SetScriptedMove(float horizontal);
        void ClearScriptedMove();
        void RequestScriptedJump();
    }
}