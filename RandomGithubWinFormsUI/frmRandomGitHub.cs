/*
MIT License

Copyright(c) 2021 Kyle Givler
https://github.com/JoyfulReaper

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Extensions.Configuration;
using RandomGithubLibrary;
using RandomGithubLibrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RandomGithubWinFormsUI
{
    public partial class frmRandomGitHub : Form
    {
        private readonly IGithubAPI githubAPI;
        private readonly IConfiguration configuration;
        private readonly Random rand = new Random();

        public frmRandomGitHub(IGithubAPI githubAPI,
            IConfiguration configuration)
        {
            InitializeComponent();
            this.githubAPI = githubAPI;
            this.configuration = configuration;

            UpdateLabels(null);
        }

        private async void btnFind_Click(object sender, EventArgs e)
        {
            IGitHubRepo repo = await GetRandomGitHub();
            UpdateLabels(repo);

            if(repo != null && repo.Id == 342959905)
            {
                MessageBox.Show("No way! I found myself!");
            }
        }

        private void UpdateLabels(IGitHubRepo repo)
        {
            lblIdValue.Text = repo?.Id.ToString() ?? "";
            lblOwnerValue.Text = repo?.Owner.Login ?? "";
            linkLabelOwner.Text = repo?.Owner.Html_url ?? "";
            lblNameValue.Text = repo?.Name ?? "";
            textBoxDesc.Text = repo?.Description ?? "";
            linkLabelUrl.Text = repo?.Html_url ?? "";
            lblLanguageValue.Text = repo?.Language ?? "";

            lblForksValue.Text = repo?.Forks_count.ToString() ?? "";
            lblStarredValue.Text = repo?.Stargazers_count.ToString() ?? "";
            lblWatchersValue.Text = repo?.Watchers_count.ToString() ?? "";

            if (githubAPI.RateLimitRemaining < 0)
            {
                lblRemainingValue.Text = "Unknown";
            }
            else
            {
                lblRemainingValue.Text = githubAPI.RateLimitRemaining.ToString();
            }
        }

        private async Task<IGitHubRepo> GetRandomGitHub()
        {
            int tryMax;

            if (int.TryParse(configuration.GetSection("MaxId").Value, out int res))
            {
                tryMax = res;
            }
            else
            {
                tryMax = 350000000;
            }

            IGitHubRepo repo = null;

            while (repo?.Name == null)
            {
                int id = rand.Next(1, tryMax);

                try
                {
                    repo = await githubAPI.GetRepo(id);
                }
                catch (RateLimitedException)
                {
                    MessageBox.Show("You cannnot make any more API calls, please try again later", "Out Of API Calls", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error Encountered during API Call:\n{ex.Message}", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Try to waste less API calls
                if (repo.Name == null)
                {
                    tryMax = id - 1;
                }
            }

            return repo;
        }

        private void linkLabelGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/JoyfulReaper") { UseShellExecute = true });
        }

        private void linkLabelOwner_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(linkLabelOwner.Text) { UseShellExecute = true });
        }

        private void linkLabelUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(linkLabelUrl.Text) { UseShellExecute = true });
        }
    }
}
