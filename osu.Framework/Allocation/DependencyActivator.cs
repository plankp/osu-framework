// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Helper class that provides methods to merge dependencies of objects and inject dependencies into objects.
    /// The process of merging/injecting dependencies into objects happens in a "bottom-up" manner from least derived to most derived.
    /// E.g. Drawable -> CompositeDrawable -> Toolbar, etc...
    /// </summary>
    /// <remarks>
    /// Injection of dependencies is ordered into two processes:
    /// <para>1) Inject into properties marked with <see cref="ResolvedAttribute"/>.</para>
    /// 2) Invoke methods marked with <see cref="BackgroundDependencyLoaderAttribute"/>.
    /// </remarks>
    internal class DependencyActivator
    {
        private static readonly ConcurrentDictionary<Type, DependencyActivator> activator_cache = new ConcurrentDictionary<Type, DependencyActivator>();

        private readonly List<InjectDependencyDelegate> injectionActivators = new List<InjectDependencyDelegate>();
        private readonly List<CacheDependencyDelegate> buildCacheActivators = new List<CacheDependencyDelegate>();

        private readonly DependencyActivator baseActivator;

        private DependencyActivator(Type type)
        {
            injectionActivators.Add(ResolvedAttribute.CreateActivator(type));
            injectionActivators.Add(BackgroundDependencyLoaderAttribute.CreateActivator(type));
            buildCacheActivators.Add(CachedAttribute.CreateActivator(type));

            if (type.BaseType != typeof(object))
                baseActivator = getActivator(type.BaseType);

            activator_cache[type] = this;
        }

        /// <summary>
        /// Injects dependencies from a <see cref="DependencyContainer"/> into an object.
        /// </summary>
        /// <param name="obj">The object to inject the dependencies into.</param>
        /// <param name="dependencies">The dependencies to use for injection.</param>
        public static void Activate(object obj, DependencyContainer dependencies)
            => getActivator(obj.GetType()).activate(obj, dependencies);

        /// <summary>
        /// Merges existing dependencies with new dependencies from an object into a new <see cref="IReadOnlyDependencyContainer"/>.
        /// </summary>
        /// <param name="obj">The object whose dependencies should be merged into the dependencies provided by <paramref name="dependencies"/>.</param>
        /// <param name="dependencies">The existing dependencies.</param>
        /// <returns>A new <see cref="IReadOnlyDependencyContainer"/> if <paramref name="obj"/> provides any dependencies, otherwise <paramref name="dependencies"/>.</returns>
        public static IReadOnlyDependencyContainer MergeDependencies(object obj, IReadOnlyDependencyContainer dependencies)
            => getActivator(obj.GetType()).mergeDependencies(obj, dependencies);

        private static DependencyActivator getActivator(Type type)
        {
            if (!activator_cache.TryGetValue(type, out var existing))
                return activator_cache[type] = new DependencyActivator(type);
            return existing;
        }

        private void activate(object obj, DependencyContainer dependencies)
        {
            baseActivator?.activate(obj, dependencies);
            injectionActivators.ForEach(a => a.Invoke(obj, dependencies));
        }

        private IReadOnlyDependencyContainer mergeDependencies(object obj, IReadOnlyDependencyContainer dependencies)
        {
            dependencies = baseActivator?.mergeDependencies(obj, dependencies) ?? dependencies;
            buildCacheActivators.ForEach(a => dependencies = a.Invoke(obj, dependencies));

            return dependencies;
        }
    }

    /// <summary>
    /// Occurs when multiple <see cref="BackgroundDependencyLoaderAttribute"/>s exist in one object.
    /// </summary>
    public class MultipleDependencyLoaderMethodsException : Exception
    {
        public MultipleDependencyLoaderMethodsException(Type type)
            : base($"The type {type.ReadableName()} has more than one method marked with a {nameof(BackgroundDependencyLoaderAttribute)}."
                   + "Any given type may only have one such method.")
        {
        }
    }

    /// <summary>
    /// Occurs when an object requests the resolution of a dependency, but the dependency doesn't exist.
    /// This is caused by the dependency not being registered by parent <see cref="CompositeDrawable"/> through
    /// <see cref="Drawable.CreateChildDependencies"/> or <see cref="CachedAttribute"/>.
    /// </summary>
    public class DependencyNotRegisteredException : Exception
    {
        public DependencyNotRegisteredException(Type type, Type requestedType)
            : base($"The type {type.ReadableName()} has a dependency on {requestedType.ReadableName()}, but the dependency is not registered.")
        {
        }
    }

    /// <summary>
    /// Occurs when dependencies dependency injection into a <see cref="Drawable"/> fails.
    /// </summary>
    internal class DependencyInjectionException : Exception
    {
        public ExceptionDispatchInfo DispatchInfo;
    }

    internal delegate void InjectDependencyDelegate(object target, DependencyContainer dependencies);
    internal delegate IReadOnlyDependencyContainer CacheDependencyDelegate(object target, IReadOnlyDependencyContainer existingDependencies);
}
