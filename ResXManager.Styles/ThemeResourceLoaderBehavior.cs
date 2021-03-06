﻿namespace tomenglertde.ResXManager.Styles
{
    using System.Windows;
    using System.Windows.Interactivity;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    public class ThemeResourceLoaderBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            var window = AssociatedObject;

            if (window == null)
                return;

            var exportProvider = window.GetExportProvider();

            var resourceProviders = exportProvider.GetExportedValues<IThemeResourceProvider>();

            foreach (var resourceProvider in resourceProviders)
            {
                resourceProvider?.LoadThemeResources(window.Resources);
            }
        }
    }
}
