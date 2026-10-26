using UnityEngine;
namespace cowsins2D
{
    public class PlayerJumpState : PlayerBaseState
    {
        public PlayerJumpState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory){}

        public override void EnterState()
        {
            player.Jump();
            player.ReduceJumpAmount();
            player.transform.up = Vector3.up;

            inputManager.onJumpCut += HandleJumpCutInput;
            inputManager.onDash += Dash;
        }

        public override void UpdateState()
        {
            player.HandleVelocities();

            // 컷신 자동 이동 중에는 조작이 꺼져 있어도 점프를 끝까지 유지한다.
            // 바로 Default 로 돌려보내면 착지 전에 isJumping 이 풀려 점프가 어색해진다.
            if (!playerControl.Controllable && !player.IsScriptedMove)
            {
                SwitchState(_factory.Default());
                return;
            }

            if (player.IsScriptedMove) player.HorizontalOrientation();
            else player.orientatePlayer?.Invoke();

            CheckSwitchState();
        }

        public override void FixedUpdateState()
        {
            if (!playerControl.Controllable && !player.IsScriptedMove) return;
            player.Movement();
        }

        public override void ExitState()
        {
            inputManager.onJumpCut -= HandleJumpCutInput;
            inputManager.onDash -= Dash;
        }

        public override void CheckSwitchState()
        {
            if (player.ladderAvailable && !player.IsOnLadderCooldown && ((player.AutoEnterLadderInAir && !player.IsGrounded) || inputManager.PlayerInputs.VerticalMovement != 0)) SwitchState(_factory.Ladder());
            
            if (player.isJumping && player.rb.linearVelocity.y < 0)
            {
                player.JumpFall();
                SwitchState(_factory.Default());
            }
            if (player.CheckIfPerformJump()) SwitchState(_factory.Jump());
            
            if (player.IsGrounded) SwitchState(_factory.Default());
        }
    }
}