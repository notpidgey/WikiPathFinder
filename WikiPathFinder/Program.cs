using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WikiPathFinder
{
    class WikiNodeItem
    {
        public WikiNodeItem()
        {
            this.Children = new List<WikiNodeItem>();
        }

        public WikiNodeItem(string path, WikiNodeItem parent)
        {
            this.Path = path;
            this.Parent = parent;
            this.Children = new List<WikiNodeItem>();
        }

        public WikiNodeItem Search(WikiNodeItem root, string nameToSearchFor)
        {
            if (nameToSearchFor == root.Path)
                return root;

            WikiNodeItem personFound = null;
            for (int i = 0; i < root.Children.Count; i++)
            {
                personFound = Search(root.Children[i], nameToSearchFor);
                if (personFound != null)
                    break;
            }

            return personFound;
        }

        public async Task UpdateChildren()
        {
            var childrenPaths = await GetAllPageLinks();

            foreach (var path in childrenPaths)
            {
                if (null == Search(GenesisNode, path))
                    Children.Add(new WikiNodeItem(path, this));
            }
        }

        private async Task<List<string>> GetAllPageLinks()
        {
            HttpResponseMessage response = await Client.GetAsync("https://en.wikipedia.org" + Path);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error: Could not read page");
                Console.ReadKey();

                Environment.Exit(1);
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(await response.Content.ReadAsStringAsync());

            var paragraphs = doc.DocumentNode
                .SelectNodes("/html/body/div[3]/div[3]/div[5]/div[1]/p");

            return paragraphs
                .SelectMany(htmlNode => htmlNode.ChildNodes
                    .Where(x => x.Attributes["href"] != null), (htmlNode, selectNode) => selectNode.Attributes["href"].Value)
                .Where(x => x.Length > 6 && x.Substring(0, 6) == "/wiki/")
                .Distinct()
                .ToList();
        }

        public WikiNodeItem GetFinal()
        {
            return Children.FirstOrDefault(x => x.Path == FinalPath);
        }

        public static HttpClient Client;
        public static string FinalPath;
        public static WikiNodeItem GenesisNode;

        public WikiNodeItem Parent { get; set; }
        public string Path { get; set; }
        public List<WikiNodeItem> Children { get; set; }
    }

    class Program
    {
        private static HttpClient wikiClient;
        private static WikiNodeItem startingNode;

        static async Task Main(string[] args)
        {
            Console.Write("Start Page: ");
            string startPage = Console.ReadLine();
            string startPath = startPage.Substring(24, startPage.Length - 24);

            Console.Write("End Page: ");
            string endPage = Console.ReadLine();
            string endPath = endPage.Substring(24, endPage.Length - 24);

            WikiNodeItem.Client = new HttpClient();
            WikiNodeItem.FinalPath = endPath;
            startingNode = new WikiNodeItem()
            {
                Path = startPath
            };
            WikiNodeItem.GenesisNode = startingNode;

            await startingNode.UpdateChildren();

            List<WikiNodeItem> generationChildren = startingNode.Children;
            while (true)
            {
                List<WikiNodeItem> nextGeneration = new List<WikiNodeItem>();
                foreach (var child in generationChildren)
                {
                    await child.UpdateChildren();
                    var final = child.GetFinal();

                    if (final != null)
                    {
                        Console.WriteLine();

                        int linksClicked = 0;
                        WikiNodeItem tree = final;
                        
                        while (tree != null)
                        {
                            linksClicked++;
                            Console.Write($"{tree.Path} <- ");
                            tree = tree.Parent;
                        }

                        Console.WriteLine($"\nLinks Clicked: {linksClicked}");

                        return;
                    }

                    nextGeneration.AddRange(child.Children);
                }

                generationChildren = nextGeneration;
            }
        }
    }
}