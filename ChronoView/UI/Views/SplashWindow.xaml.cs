using System;
using System.Windows;
using System.Windows.Threading;

namespace ChronoView.UI.Views
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer? _dotTimer;
        private int _dotCount = 0;

        public SplashWindow()
        {
            InitializeComponent();
            StartLoadingAnimation();
        }

        /// <summary>
        /// "Loading..." 애니메이션 시작
        /// </summary>
        private void StartLoadingAnimation()
        {
            _dotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _dotTimer.Tick += (s, e) =>
            {
                _dotCount = (_dotCount + 1) % 4;
                LoadingDots.Text = new string('.', _dotCount == 0 ? 3 : _dotCount);
            };

            _dotTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _dotTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
