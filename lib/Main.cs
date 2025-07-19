using SplashKitSDK;
using ShapeDrawer.lib;

public enum AppState
{
    Menu,
    MapModeSelect,
    Game
}

public static class Program
{
    public static void Main()
    {
        SplashKit.OpenWindow("Game Window", 1536, 1152);
        int selectedMapIndex = 0; // 0 = Map 1, 1 = Map 2

        AppState state = AppState.Menu;
        GameForm game = null;
        Bitmap menuBg = SplashKit.LoadBitmap("MenuBg", @"Resources/menu.png");

        // Button rectangles
        Rectangle startBtn = new Rectangle() { X = 568, Y = 400, Width = 400, Height = 100 };
        Rectangle exitBtn = new Rectangle() { X = 568, Y = 550, Width = 400, Height = 100 };

        while (!SplashKit.WindowCloseRequested("Game Window"))
        {
            SplashKit.ProcessEvents();
            SplashKit.ClearScreen(Color.Black);

            if (state == AppState.Menu)
            {
                // Draw menu background
                SplashKit.DrawBitmap(menuBg, 0, 0);

                Point2D mouse = SplashKit.MousePosition();

                // Start button hover
                Color startColor = SplashKit.PointInRectangle(mouse, startBtn) ? Color.Yellow : Color.White;
                SplashKit.FillRectangle(startColor, startBtn);
                SplashKit.DrawText("Start", Color.Black, "Arial", 48, startBtn.X + 120, startBtn.Y + 25);

                // Exit button hover
                Color exitColor = SplashKit.PointInRectangle(mouse, exitBtn) ? Color.Yellow : Color.White;
                SplashKit.FillRectangle(exitColor, exitBtn);
                SplashKit.DrawText("Exit", Color.Black, "Arial", 48, exitBtn.X + 140, exitBtn.Y + 25);

                // Handle button clicks
                if (SplashKit.MouseClicked(MouseButton.LeftButton))
                {
                    Point2D mouseClick = SplashKit.MousePosition();
                    if (SplashKit.PointInRectangle(mouseClick, startBtn))
                    {
                        state = AppState.MapModeSelect;
                    }

                    else if (SplashKit.PointInRectangle(mouseClick, exitBtn))
                    {
                        break;
                    }
                }

                SplashKit.RefreshScreen(); // Only refresh here in menu
            }
            else if (state == AppState.MapModeSelect)
            {
                // --- UI rectangles ---
                Rectangle map1Rect = new Rectangle() { X = 100, Y = 220, Width = 300, Height = 80 };
                Rectangle map2Rect = new Rectangle() { X = 100, Y = 320, Width = 300, Height = 80 };
                Rectangle map3Rect = new Rectangle() { X = 100, Y = 420, Width = 300, Height = 80 };
                Rectangle map4Rect = new Rectangle() { X = 100, Y = 520, Width = 300, Height = 80 };
                Rectangle map5Rect = new Rectangle() { X = 100, Y = 620, Width = 300, Height = 80 };
                Rectangle map6Rect = new Rectangle() { X = 100, Y = 720, Width = 300, Height = 80 };
                Rectangle normalModeRect = new Rectangle() { X = 500, Y = 300, Width = 300, Height = 80 };
                Rectangle infiniteModeRect = new Rectangle() { X = 500, Y = 420, Width = 300, Height = 80 };

                SplashKit.DrawBitmap(menuBg, 0, 0);

                // Draw map selection (left)
                Point2D mouse = SplashKit.MousePosition();
                Rectangle[] mapRects = { map1Rect, map2Rect, map3Rect, map4Rect, map5Rect, map6Rect };
                string[] mapNames = { "Map 1", "Map 2", "Map 3", "Map 4", "Map 5", "Map 6" };

                for (int i = 0; i < mapRects.Length; i++)
                {
                    bool hover = SplashKit.PointInRectangle(mouse, mapRects[i]);
                    Color fill = hover || selectedMapIndex == i ? Color.Yellow : Color.LightGray;
                    SplashKit.FillRectangle(fill, mapRects[i]);
                    SplashKit.DrawText(mapNames[i], Color.Black, "Arial", 22, mapRects[i].X + 40, mapRects[i].Y + 25);
                }


                // Draw mode selection (right)
                bool normalHover = SplashKit.PointInRectangle(mouse, normalModeRect);
                bool infiniteHover = SplashKit.PointInRectangle(mouse, infiniteModeRect);

                SplashKit.FillRectangle(normalHover ? Color.Yellow : Color.Green, normalModeRect);
                SplashKit.DrawText("Normal Mode", Color.Black, "Arial", 28, normalModeRect.X + 40, normalModeRect.Y + 20);

                SplashKit.FillRectangle(infiniteHover ? Color.Yellow : Color.Red, infiniteModeRect);
                SplashKit.DrawText("Infinite Mode", Color.Black, "Arial", 28, infiniteModeRect.X + 40, infiniteModeRect.Y + 20);

                // Handle button clicks
                if (SplashKit.MouseClicked(MouseButton.LeftButton))
                {
                    for (int i = 0; i < mapRects.Length; i++)
                    {
                        if (SplashKit.PointInRectangle(mouse, mapRects[i]))
                        {
                            selectedMapIndex = i;
                        }
                    }
                    if (SplashKit.PointInRectangle(mouse, normalModeRect))
                    {
                        game = new GameForm(false, selectedMapIndex);
                        state = AppState.Game;
                    }
                    else if (SplashKit.PointInRectangle(mouse, infiniteModeRect))
                    {
                        game = new GameForm(true, selectedMapIndex);
                        state = AppState.Game;
                    }
                }



                SplashKit.RefreshScreen();
            }
            else if (state == AppState.Game)
            {
                game.HandleMouseInput();
                game.Update();
                game.Draw(); // This already calls SplashKit.RefreshScreen()

                // Check if game requested return to menu
                if (game.ShouldReturnToMenu)
                {
                    state = AppState.Menu;
                    game = null;
                }
            }
        }

    }
}

