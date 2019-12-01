// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="CharacterActorBehaviour"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Resource prefab should have a <see cref="CharacterActorBehaviour"/> component attached to the root object.
    /// Apperance and other property changes are routed via the events of <see cref="CharacterActorBehaviour"/> component.
    /// </remarks>
    public class GenericCharacter : GenericActor<CharacterActorBehaviour>, ICharacterActor
    {
        public CharacterLookDirection LookDirection { get => lookDirection; set => SetLookDirection(value); }

        private readonly TextPrinterManager textPrinterManager;
        private CharacterLookDirection lookDirection;

        public GenericCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata)
        {
            textPrinterManager = Engine.GetService<TextPrinterManager>();
            textPrinterManager.OnPrintTextStarted += HandlePrintTextStarted;
            textPrinterManager.OnPrintTextFinished += HandlePrintTextFinished;
        }

        public async override Task InitializeAsync ()
        {
            await base.InitializeAsync();

            Behaviour.InvokeIsSpeakingChangedEvent(false);
        }

        public async Task ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, 
            EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.lookDirection = lookDirection;

            Behaviour.InvokeLookDirectionChangedEvent(lookDirection);

            if (Behaviour.TransformByLookDirection)
            {
                var rotation = LookDirectionToRotation(lookDirection);
                await ChangeRotationAsync(rotation, duration, easingType, cancellationToken);
            }
        }

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            this.lookDirection = lookDirection;

            Behaviour.InvokeLookDirectionChangedEvent(lookDirection);

            if (Behaviour.TransformByLookDirection)
            {
                var rotation = LookDirectionToRotation(lookDirection);
                SetBehaviourRotation(rotation);
            }
        }

        protected virtual Quaternion LookDirectionToRotation (CharacterLookDirection lookDirection)
        {
            var yAngle = 0f;
            switch (lookDirection)
            {
                case CharacterLookDirection.Center:
                    yAngle = 0;
                    break;
                case CharacterLookDirection.Left:
                    yAngle = Behaviour.LookDeltaAngle;
                    break;
                case CharacterLookDirection.Right:
                    yAngle = -Behaviour.LookDeltaAngle;
                    break;
            }

            var currentRotation = Rotation.eulerAngles;
            return Quaternion.Euler(currentRotation.x, yAngle, currentRotation.z);
        }

        public override void Dispose ()
        {
            base.Dispose();

            if (textPrinterManager != null)
            {
                textPrinterManager.OnPrintTextStarted -= HandlePrintTextStarted;
                textPrinterManager.OnPrintTextFinished -= HandlePrintTextFinished;
            }
        }

        private void HandlePrintTextStarted (PrintTextArgs args)
        {
            if (args.AuthorId == Id)
                Behaviour.InvokeIsSpeakingChangedEvent(true);
        }

        private void HandlePrintTextFinished (PrintTextArgs args)
        {
            if (args.AuthorId == Id)
                Behaviour.InvokeIsSpeakingChangedEvent(false);
        }
    }
}
