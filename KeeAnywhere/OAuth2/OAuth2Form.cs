﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using KeePass.UI;

namespace KeeAnywhere.OAuth2
{
    public partial class OAuth2Form : Form
    {
        private IOAuth2Provider m_provider;
        private bool m_isPreAuthorization;
        private bool m_isClaimed;

        public OAuth2Form()
        {
            InitializeComponent();
        }

        public async void InitEx(IOAuth2Provider provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            m_provider = provider;


            await provider.Initialize();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            GlobalWindowManager.AddWindow(this);

            Icon = PluginResources.Icon_OneDrive_16x16;

            UpdateBanner();

            m_isPreAuthorization = m_provider.PreAuthorizationUrl != null;
            m_browser.Navigate(m_provider.PreAuthorizationUrl ?? m_provider.AuthorizationUrl);
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            GlobalWindowManager.RemoveWindow(this);
        }

        private void UpdateBanner()
        {
            if (m_provider == null) return;
            var text = string.Format("Authorize to {0}", m_provider.FriendlyProviderName);
            this.Text = text;
            BannerFactory.CreateBannerEx(this, m_bannerImage,
                PluginResources.KeeAnywhere_48x48, text,
                string.Format("Please follow the instructions to authorize KeeAnywhere to access your {0} account.", m_provider.FriendlyProviderName));
        }

        private void OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted " + e.Url);
        }

        private async void OnNavigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            Debug.WriteLine("Navigated " + e.Url);

            // Pre-Authorization performed?
            if (m_isPreAuthorization)
            {
                m_isPreAuthorization = false;
                m_browser.Stop();
                m_browser.Navigate(m_provider.AuthorizationUrl);
                return;
            }

            // we need to ignore all navigation that is already claimed or could not be claimed (due to missing code).
            if (m_isClaimed || !m_provider.CanClaim(e.Url, m_browser.DocumentTitle))
            {
                return;
            }

            m_isClaimed = true;
            m_browser.Stop();
            m_browser.Visible = false;
            m_pnlWait.Visible = true;

            try
            {
                var isOk = await m_provider.Claim(e.Url, m_browser.DocumentTitle);
                DialogResult = isOk ? DialogResult.OK : DialogResult.Abort;
            }
            catch (Exception ex)
            {
                this.LastException = ex;
                DialogResult = DialogResult.Abort;
            }
            finally
            {
                Close();
            }
        }

        public Exception LastException { get; set; }

        private void OnNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            Debug.WriteLine("Navigating " + e.Url);
        }

        private void OnNewWindow(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("NewWindow");
            e.Cancel = true;
        }

        private void OnResize(object sender, EventArgs e)
        {
            UpdateBanner();
        }
    }
}