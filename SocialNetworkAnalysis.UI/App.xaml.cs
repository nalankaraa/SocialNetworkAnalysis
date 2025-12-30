using Microsoft.Extensions.DependencyInjection;
using SocialNetworkAnalysis.Algorithms.Analysis;
using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Services;
using SocialNetworkAnalysis.Services.Calculation;
using SocialNetworkAnalysis.Services.Persistence;
using SocialNetworkAnalysis.UI.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SocialNetworkAnalysis.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider Services { get; }

        public App()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<IWeightCalculator, WeightCalculator>();
            services.AddSingleton<IHeuristicProvider, EuclideanHeuristicProvider>();
            services.AddSingleton<IGraphService, GraphService>();
            services.AddTransient<CsvDataSeeder>();

            // Algorithms
            services.AddSingleton<AlgorithmFactory>();
            services.AddSingleton<AnalysisAlgorithmFactory>();


            services.AddSingleton<AlgorithmBase, SocialNetworkAnalysis.Algorithms.Traversal.BFSAlgorithm>();
            services.AddSingleton<AlgorithmBase, SocialNetworkAnalysis.Algorithms.Traversal.DFSAlgorithm>();
            services.AddSingleton<AlgorithmBase, SocialNetworkAnalysis.Algorithms.ShortestPath.DijkstraAlgorithm>();
            services.AddSingleton<AlgorithmBase, SocialNetworkAnalysis.Algorithms.ShortestPath.AStarAlgorithm>();
            services.AddSingleton<AlgorithmBase, SocialNetworkAnalysis.Algorithms.Coloring.WelshPowellColoring>();
            services.AddSingleton<AnalysisAlgorithmBase, SocialNetworkAnalysis.Algorithms.Analysis.ConnectedComponentsAlgorithm>();
            services.AddSingleton<AnalysisAlgorithmBase, SocialNetworkAnalysis.Algorithms.Analysis.DegreeCentralityAlgorithm>();
            services.AddSingleton<AnalysisAlgorithmBase, SocialNetworkAnalysis.Algorithms.Analysis.ClubCommunityDetectionAlgorithm>();

            // Factories
            services.AddSingleton<AlgorithmFactory>();
            services.AddSingleton<AnalysisAlgorithmFactory>();


            // ViewModels
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Start-up Error: {ex.Message}", "Error");
            }
        }
    }

}
