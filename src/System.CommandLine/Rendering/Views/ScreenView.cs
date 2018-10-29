﻿using System.Threading;

namespace System.CommandLine.Rendering.Views
{
    public class ScreenView : IDisposable
    {
        private View _child;
        private int _renderRequested;
        private int _renderInProgress;
        private readonly SynchronizationContext _context;

        public ScreenView(
            ConsoleRenderer renderer,
            SynchronizationContext synchronizationContext = null)
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _context = synchronizationContext ?? SynchronizationContext.Current ?? new SynchronizationContext();
            Console = renderer.Console;
        }

        private IConsole Console { get; }
        private ConsoleRenderer Renderer { get; }

        public View Child
        {
            get => _child;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var previousChild = _child;
                if (previousChild != null)
                {
                    previousChild.Updated -= ChildUpdated;
                }

                _child = value;

                value.Updated += ChildUpdated;
            }
        }

        private void ChildUpdated(object sender, EventArgs e)
        {
            if (Interlocked.CompareExchange(ref _renderRequested, 1, 0) == 0)
            {
                _context.Post(x =>
                {
                    while (Interlocked.CompareExchange(ref _renderRequested, 0, 1) == 1)
                    {
                        if (Interlocked.CompareExchange(ref _renderInProgress, 1, 0) == 0)
                        {
                            Render();
                            Interlocked.Exchange(ref _renderInProgress, 0);
                        }
                    }
                }, null);
            }
        }

        // may not want this?
        public void Render(Region region)
        {
            Child?.Render(Renderer, region);
        }

        public void Render()
        {
            var region = Console.GetRegion();

            Render(new Region(0, 0, region.Width, region.Height));
        }

        public void Dispose()
        {
            if (_child is View child)
            {
                child.Updated -= ChildUpdated;
            }
        }
    }
}