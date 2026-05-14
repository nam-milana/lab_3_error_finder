// Views/MainWindow.xaml.cs
using ErrorFinder.Engine;
using ErrorFinder.Processors;
using ErrorFinder.Providers;
using ErrorFinder.ViewModels;

namespace ErrorFinder.Views;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();

        var provider = new TxtFileProvider();
        var processor = new ErrorSearchProcessor();
        var engine = new FileProcessingEngine(workerCount: 4);

        var viewModel = new MainViewModel(provider, processor, engine);

        DataContext = viewModel;
    }
}
