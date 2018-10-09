// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    /// <summary>
    /// Tracks the rendering state associated with an <see cref="IComponent"/> instance
    /// within the context of a <see cref="Renderer"/>. This is an internal implementation
    /// detail of <see cref="Renderer"/>.
    /// </summary>
    internal class ComponentState
    {
        private readonly int _componentId; // TODO: Change the type to 'long' when the Mono runtime has more complete support for passing longs in .NET->JS calls
        private readonly ComponentState _parentComponentState;
        private readonly IComponent _component;
        private readonly Renderer _renderer;
        private RenderTreeBuilder _renderTreeBuilderCurrent;
        private RenderTreeBuilder _renderTreeBuilderPrevious;
        private bool _componentWasDisposed;

        public IComponent Component => _component;
        public ComponentState ParentComponentState => _parentComponentState;

        /// <summary>
        /// Constructs an instance of <see cref="ComponentState"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> with which the new instance should be associated.</param>
        /// <param name="componentId">The externally visible identifier for the <see cref="IComponent"/>. The identifier must be unique in the context of the <see cref="Renderer"/>.</param>
        /// <param name="component">The <see cref="IComponent"/> whose state is being tracked.</param>
        /// <param name="parentComponentState">The <see cref="ComponentState"/> for the parent component, or null if this is a root component.</param>
        public ComponentState(Renderer renderer, int componentId, IComponent component, ComponentState parentComponentState)
        {
            _componentId = componentId;
            _parentComponentState = parentComponentState;
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _renderTreeBuilderCurrent = new RenderTreeBuilder(renderer);
            _renderTreeBuilderPrevious = new RenderTreeBuilder(renderer);
        }

        public void RenderIntoBatch(RenderBatchBuilder batchBuilder, RenderFragment renderFragment)
        {
            // A component might be in the render queue already before getting disposed by an
            // earlier entry in the render queue. In that case, rendering is a no-op.
            if (_componentWasDisposed)
            {
                return;
            }

            // Swap the old and new tree builders
            (_renderTreeBuilderCurrent, _renderTreeBuilderPrevious) = (_renderTreeBuilderPrevious, _renderTreeBuilderCurrent);

            _renderTreeBuilderCurrent.Clear();
            renderFragment(_renderTreeBuilderCurrent);

            var diff = RenderTreeDiffBuilder.ComputeDiff(
                _renderer,
                batchBuilder,
                _componentId,
                _renderTreeBuilderPrevious.GetFrames(),
                _renderTreeBuilderCurrent.GetFrames());
            batchBuilder.UpdatedComponentDiffs.Append(diff);
        }

        public void DisposeInBatch(RenderBatchBuilder batchBuilder)
        {
            _componentWasDisposed = true;
 
            // TODO: Handle components throwing during dispose. Shouldn't break the whole render batch.
            if (_component is IDisposable disposable)
            {
                disposable.Dispose();
            }

            RenderTreeDiffBuilder.DisposeFrames(batchBuilder, _renderTreeBuilderCurrent.GetFrames());
        }

        public void DispatchEvent(EventHandlerInvoker binding, UIEventArgs eventArgs)
        {
            if (_component is IHandleEvent handleEventComponent)
            {
                handleEventComponent.HandleEvent(binding, eventArgs);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The component of type {_component.GetType().FullName} cannot receive " +
                    $"events because it does not implement {typeof(IHandleEvent).FullName}.");
            }
        }

        public void NotifyRenderCompleted()
            => (_component as IHandleAfterRender)?.OnAfterRender();

        // TODO: Remove this once we can remove TemporaryGetParentComponentIdForTest
        // from Renderer.cs and corresponding unit test.
        public int? TemporaryParentComponentIdForTests
            => _parentComponentState?._componentId;
    }
}
