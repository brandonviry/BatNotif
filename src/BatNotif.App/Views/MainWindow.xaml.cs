using System.ComponentModel;
using System.Windows;
using BatNotif.App.ViewModels;

namespace BatNotif.App.Views;

/// <summary>
/// Fenêtre principale (ui-contract §3). La fermer la masque seulement : l'application continue dans la zone
/// de notification (FR-030).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>Mis à vrai par l'application juste avant l'arrêt pour autoriser la vraie fermeture.</summary>
    public bool AllowClose { get; set; }

    /// <summary>Affiche la fenêtre au premier plan, restaurée si elle était réduite.</summary>
    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }
}
