using UnityEngine;

namespace cowsins2D
{
    public class PlayerDefaultState : PlayerBaseState
    {
        public PlayerDefaultState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory){}

        public override void EnterState()
        {
            // Reset Jumping
            player.isJumping = false;

            inputManager.onJump += HandleJumpInput;
            inputManager.onDash += Dash;
        }

        public override void UpdateState()
        {
            player.CheckCollisions();
            player.HandleVelocities();

            // Avoid running the following code if the player is not controllable
            if (!playerControl.Controllable)
            {
                // 컷신 자동 이동 중에는 방향 전환과 발소리까지는 살려둔다.
                // 조준 기반 방향 전환은 컷신에서 엉뚱한 곳을 보게 되므로 이동 방향만 따른다.
                if (player.IsScriptedMove)
                {
                    player.HorizontalOrientation();
                    player.HandleFootsteps();

                    // 막혔을 때 RequestScriptedJump 로 채워진 점프 버퍼를 여기서 소비한다.
                    // 평소 입력은 HandleJumpInput 이 걸러 내므로 이 경로로는 들어오지 않는다.
                    if (player.CheckIfPerformJump()) SwitchState(_factory.Jump());
                }
                return;
            }
            player.CheckIfJumpingOrWallSliding();
            player.orientatePlayer?.Invoke();
            player.HandleFootsteps();
            CheckSwitchState();
        }

        public override void FixedUpdateState()
        {
            // Avoid running the following code if the player is not controllable
            // 단, 컷신 자동 이동 중이면 조작이 꺼져 있어도 이동은 돌려야 한다.
            if (!playerControl.Controllable && !player.IsScriptedMove) return;
            // Handles the player movement
            player.Movement();
        }

        public override void ExitState()
        {
            inputManager.onJump -= HandleJumpInput;
            inputManager.onDash -= Dash;
        }

        public override void CheckSwitchState()
        {
            // Check ladder entry. It must have a ladder available and not be on cooldown
            if (player.ladderAvailable && !player.IsOnLadderCooldown && ((player.AutoEnterLadderInAir && !player.IsGrounded) || inputManager.PlayerInputs.VerticalMovement > 0))
            {
                SwitchState(_factory.Ladder());
                return;
            }

            if(player.CheckIfPerformJump()) SwitchState(_factory.Jump());

            if (player.CheckSlideStatus()) SwitchState(_factory.WallSlide());

            if (inputManager.PlayerInputs.Crouch && player.AllowCrouch && player.CurrentLadder == null) SwitchState(_factory.Crouch());

            if (inputManager.PlayerInputs.Jump && player.currentJumps <= 0 && player.LastOnGroundTime <= 0 && player.LastPressedJumpTime > 0 && player.CanGlide) SwitchState(_factory.Glide());
        }
    }
}