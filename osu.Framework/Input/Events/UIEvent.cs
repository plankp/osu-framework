// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Input.States;
using OpenTK;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Denotes a UI event.
    /// A UI event is produced for and can be handled by a <see cref="Drawable"/>.
    /// While handling events, the <see cref="Target"/> is set to the <see cref="Drawable"/> which is handling the event.
    /// </summary>
    public abstract class UIEvent
    {
        /// <summary>
        /// The current input state.
        /// </summary>
        /// <remarks>
        /// This raw state should not be used for event handling if not really needed.
        /// Instead, properties such as <see cref="MousePosition"/> and event data should be used.
        /// </remarks>
        [NotNull]
        public readonly InputState CurrentState;

        /// <summary>
        /// The current target <see cref="Drawable"/> of this event.
        /// This can be modified to reuse an instance many times.
        /// </summary>
        [CanBeNull]
        public Drawable Target;

        /// <summary>
        /// Convert a coordinate to <see cref="Target"/>'s parent space.
        /// </summary>
        protected Vector2 ToLocalSpace(Vector2 screenSpacePosition) => Target?.Parent?.ToLocalSpace(screenSpacePosition) ?? screenSpacePosition;

        /// <summary>
        /// The current mouse position in screen space.
        /// </summary>
        public Vector2 ScreenSpaceMousePosition => CurrentState.Mouse.Position;

        /// <summary>
        /// The current mouse position in <see cref="Target"/>'s parent space.
        /// </summary>
        public Vector2 MousePosition => ToLocalSpace(ScreenSpaceMousePosition);

        /// <summary>
        /// Used for compatibility only. Will be removed after the new way is applied to code.
        /// Used as a proxy of legacy code dealing with an <see cref="InputState"/> to apply <see cref="Target"/> choordinate conversion.
        /// </summary>
        internal InputState LegacyInputState
        {
            get
            {
                var state = CurrentState.Clone();
                state.Mouse = new LocalMouseState(state.Mouse.NativeState, Target);
                return state;
            }
        }

        protected UIEvent([NotNull] InputState state)
        {
            CurrentState = state ?? throw new ArgumentNullException(nameof(state));
        }

        public override string ToString() => $"{GetType().ReadableName()}()";
    }
}
