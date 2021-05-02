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
using Microsoft.Extensions.DependencyInjection;
using RandomGithubLibrary;
using RandomGithubLibrary.Models;
using System;
using System.Threading.Tasks;

namespace RandomGithubConsoleUI
{
    class Program
    {
        private static readonly Random rand = new Random();
        private static int tryMax = -1;

        static async Task Main(string[] args)
        {
            var serviceProvider = Bootstrap.Initialize(args);

            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var api = serviceProvider.GetRequiredService<IGithubAPI>();

            api.Initialize();
            IGitHubRepo repo = null;

            bool again = true;

            while (again)
            {
                if (int.TryParse(config.GetSection("MaxId").Value, out int res))
                {
                    tryMax = res;
                }
                else
                {
                    tryMax = 300000000;
                }

                while (repo?.Name == null)
                {
                    int id = rand.Next(1, tryMax);

                    try
                    {
                        repo = await api.GetRepo(id);
                    }
                    catch (RateLimitedException)
                    {
                        Console.WriteLine("You cannot make any more API calls due to rate limiting. Try again later.");
                        Environment.Exit(-1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Something went wrong with the API call!");
                        Console.WriteLine(ex.Message);
                    }

                    // Try to waste less API calls
                    if (repo.Name == null)
                    {
                        tryMax = id - 1;
                    }
                    else
                    {
                        DisplayRepoInfo(repo);
                        Console.WriteLine();
                        DisplayRateLimitingInfo(api);
                    }
                }

                Console.WriteLine();
                Console.Write("Would you like to find another (Y/n): ");
                var input = Console.ReadLine();
                if(!string.IsNullOrEmpty(input) && Char.ToUpperInvariant(input[0]) == 'N')
                {
                    again = false;
                }
                repo = null;
                Console.WriteLine();
            }

        }

        private static void DisplayRepoInfo(IGitHubRepo repo)
        {
            Console.WriteLine("I found this random GitHub just for you!");
            Console.WriteLine($"Id: {repo.Id}");
            Console.WriteLine($"Owner: {repo.Owner.Login}");
            Console.WriteLine($"Owner's GitHub: {repo.Owner.Html_url}");
            Console.WriteLine($"Name: {repo.Name}");
            Console.WriteLine($"Description: {repo.Description}");
            Console.WriteLine($"URL: {repo.Html_url}");
            Console.WriteLine($"Programming Language: {repo.Language}");
        }

        private static void DisplayRateLimitingInfo(IGithubAPI gh)
        {
            Console.WriteLine($"API calls until limited: {gh.RateLimitRemaining}");
        }
    }
}