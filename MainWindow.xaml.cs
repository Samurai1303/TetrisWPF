using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TetrisWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ImageSource[] tileImages = new ImageSource[]
        {
            new BitmapImage(new Uri("Assets/Images/TileEmpty.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TileCyan.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TileBlue.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TileOrange.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TileYellow.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TileGreen.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TilePurple.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/TileRed.png", UriKind.Relative))
        };

        private readonly ImageSource[] blockImages = new ImageSource[]
        {
            new BitmapImage(new Uri("Assets/Images/Block-Empty.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-I.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-J.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-L.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-O.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-S.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-T.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Images/Block-Z.png", UriKind.Relative))
        };

        private readonly Image[,] imageControls;
        private readonly int maxDelay = 1000;
        private readonly int minDelay = 175;
        private readonly int delayDecrease = 15;
        private bool Terminate = false;

        private GameState gameState = new GameState();

        public MainWindow ()
        {
            InitializeComponent();
            imageControls = SetupGameCanvas(gameState.GameGrid);
        }

        private Image[,] SetupGameCanvas (GameGrid grid)
        {
            Image[,] imageControls = new Image[grid.Rows, grid.Columns];
            int cellSize = 25;

            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Columns; c++)
                {
                    Image imageControl = new Image
                    {
                        Width = cellSize,
                        Height = cellSize
                    };

                    Canvas.SetTop(imageControl, (r - 2) * cellSize + 10);
                    Canvas.SetLeft(imageControl, c * cellSize);
                    GameCanvas.Children.Add(imageControl);
                    imageControls[r, c] = imageControl;
                }
            }

            return imageControls;
        }

        private void DrawGrid (GameGrid grid)
        {
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Columns; c++)
                {
                    int id = grid[r, c];
                    imageControls[r, c].Opacity = 1;
                    imageControls[r, c].Source = tileImages[id];
                }
            }
        }

        private void DrawBlock (Block block)
        {
            foreach (Position p in block.TilePositions())
            {
                imageControls[p.Row, p.Column].Opacity = 1;
                imageControls[p.Row, p.Column].Source = tileImages[block.Id];
            }
        }

        private void DrawNextBlock (BlockQueue blockQueue)
        {
            Block next = blockQueue.NextBlock;
            NextImage.Source = blockImages[next.Id];
        }

        private void DrawHeldBlock (Block heldBlock)
        {
            if (heldBlock == null)
            {
                HoldImage.Source = blockImages[0];
            }
            else
            {
                HoldImage.Source = blockImages[heldBlock.Id];
            }
        }

        private void DrawGhostBlock (Block block)
        {
            int dropDistance = gameState.BlockDropDistance();

            foreach (Position p in block.TilePositions())
            {
                imageControls[p.Row + dropDistance, p.Column].Opacity = 0.25;
                imageControls[p.Row + dropDistance, p.Column].Source = tileImages[block.Id];
            }
        }

        private void Draw (GameState gameState)
        {
            DrawGrid(gameState.GameGrid);
            DrawGhostBlock(gameState.CurrentBlock);
            DrawBlock(gameState.CurrentBlock);
            DrawNextBlock(gameState.BlockQueue);
            DrawHeldBlock(gameState.HeldBlock);
            HighScoreText.Text = $"High Score: {gameState.HighScore}";
            ScoreText.Text = $"Score: {gameState.Score}";
        }

        private async Task GameLoop ()
        {
            Draw(gameState);

            while (!gameState.GameOver)
            {
                while (gameState.Paused)
                {
                    await Task.Delay(1);
                }

                if (Terminate)
                {
                    return;
                }

                int delay = Math.Max(minDelay, maxDelay - (gameState.LinesCleared * delayDecrease));
                await Task.Delay(delay);
                gameState.MoveBlockDown();
                Draw(gameState);
            }

            GameOverMenu.Visibility = Visibility.Visible;
            FinalScoreText.Text = $"Score: {gameState.Score}";
        }

        private void Window_KeyDown (object sender, KeyEventArgs e)
        {
            if (gameState.GameOver)
            {
                return;
            }
            else if (gameState.Paused)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                    gameState.MoveBlockLeft();
                    break;
                case Key.Right:
                    gameState.MoveBlockRight();
                    break;
                case Key.Down:
                    gameState.MoveBlockDown();
                    break;
                case Key.Up:
                    gameState.RotateBlockCW();
                    break;
                case Key.Z:
                    gameState.RotateBlockCCW();
                    break;
                case Key.C:
                    gameState.HoldBlock();
                    break;
                case Key.Space:
                    gameState.DropBlock();
                    break;
                default:
                    return;
            }

            Draw(gameState);
        }

        private async void GameCanvas_Loaded (object sender, RoutedEventArgs e)
        {
            await GameLoop();
        }

        private async void PlayAgain_Click (object sender, RoutedEventArgs e)
        {
            Terminate = true;
            gameState = new GameState();
            await GameLoop();
            Terminate = false;
        }

        private void Quit_Click (object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Pause_Click (object sender, RoutedEventArgs e)
        {
            PauseMenu.Visibility = Visibility.Visible;
            gameState.Paused = true;
        }

        private void Resume_Click (object sender, EventArgs e)
        {
            PauseMenu.Visibility = Visibility.Hidden;
            gameState.Paused = false;
        }

        private async void GameOverPlayAgain_Click (object sender, RoutedEventArgs e)
        {
            gameState = new GameState();
            GameOverMenu.Visibility = Visibility.Hidden;
            await GameLoop();
        }
    }
}
