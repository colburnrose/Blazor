// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;
using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// An enumerator that iterates through a <see cref="ParameterCollection"/>.
    /// </summary>
    public struct ParameterEnumerator
    {
        RenderTreeFrameParameterEnumerator _renderTreeParamsEnumerator;

        internal ParameterEnumerator(RenderTreeFrame[] frames, int ownerIndex)
        {
            _renderTreeParamsEnumerator = new RenderTreeFrameParameterEnumerator(frames, ownerIndex);
        }

        /// <summary>
        /// Gets the current value of the enumerator.
        /// </summary>
        public Parameter Current
            => _renderTreeParamsEnumerator.Current;

        /// <summary>
        /// Instructs the enumerator to move to the next value in the sequence.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
            => _renderTreeParamsEnumerator.MoveNext();

        struct RenderTreeFrameParameterEnumerator
        {
            private readonly RenderTreeFrame[] _frames;
            private readonly int _ownerIndex;
            private readonly int _ownerDescendantsEndIndexExcl;
            private int _currentIndex;

            internal RenderTreeFrameParameterEnumerator(RenderTreeFrame[] frames, int ownerIndex)
            {
                _frames = frames;
                _ownerIndex = ownerIndex;
                _ownerDescendantsEndIndexExcl = ownerIndex + _frames[ownerIndex].ElementSubtreeLength;
                _currentIndex = ownerIndex;
            }

            
            public Parameter Current
            {
                get
                {
                    if (_currentIndex > _ownerIndex)
                    {
                        ref var frame = ref _frames[_currentIndex];
                        return new Parameter(frame.AttributeName, frame.AttributeValue);
                    }
                    else
                    {
                        throw new InvalidOperationException("Iteration has not yet started.");
                    }
                }
            }

            public bool MoveNext()
            {
                // Stop iteration if you get to the end of the owner's descendants...
                var nextIndex = _currentIndex + 1;
                if (nextIndex == _ownerDescendantsEndIndexExcl)
                {
                    return false;
                }

                // ... or if you get to its first non-attribute descendant (because attributes
                // are always before any other type of descendant)
                if (_frames[nextIndex].FrameType != RenderTreeFrameType.Attribute)
                {
                    return false;
                }

                _currentIndex = nextIndex;
                return true;
            }
        }
    }
}
