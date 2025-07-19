using System.Numerics;
using SplashKitSDK;
using System;
using System.Collections.Generic;
using System.Linq;
namespace ShapeDrawer.lib
{
    public class GameForm
    {
        public bool _infiniteMode = false;
        public int _waveSetCount = 0;
        public double _enemyHealthMultiplier = 1.0;
        public int _selectedMapIndex = 0; // 0 = first map, 1 = second map

        private Rectangle _speedButtonRect = new Rectangle() { X = 1536 - 100, Y = 10, Width = 40, Height = 40 };
        public bool ShouldReturnToMenu { get; private set; }
        private Music _backgroundMusic;
        private bool _musicLoaded = false;
        private int _lastMoney = -1;
        private bool _isPaused = false;
        private Rectangle _pauseButtonRect = new Rectangle() { X = 1536 - 50, Y = 10, Width = 40, Height = 40 };
        private Rectangle _resumeButtonRect = new Rectangle() { X = 100, Y = 100, Width = 200, Height = 60 };
        private Rectangle _restartButtonRect = new Rectangle() { X = 100, Y = 180, Width = 200, Height = 60 };
        private Rectangle _homeButtonRect = new Rectangle() { X = 100, Y = 260, Width = 200, Height = 60 };
        private List<(Vector2 pos, string text, Color color, double timer)> _floatingTexts = new();
        private Bitmap _baseImage;
        private Bitmap _mapImage1;
        private Bitmap _mapImage2;
        private Bitmap _mapImage3;
        private Bitmap _mapImage4;
        private Bitmap _mapImage5;
        private Bitmap _mapImage6;
        private Bitmap _mapImage;

        private int _currentWave = 1;
        private const int _totalWaves = 15;
        private enum EndGameState { None, Victory, Defeat }
        private EndGameState _endGameState = EndGameState.None;

        private enum TowerType { None, Gunner, Shotgunner, Sniper, Farm, Splash, Freeze, Poison, Laser }
        private TowerType _selectedTowerType = TowerType.None;
        private Rectangle _gunnerRect, _shotgunnerRect, _sniperRect, _farmRect, _splashRect, _freezeRect, _poisonRect, _laserRect;
        private Tower _selectedTower = null;
        private bool _showSkipPopup = false;
        private Rectangle _skipPopupRect = new Rectangle() { X = 1250, Y = 800, Width = 220, Height = 60 };
        private DateTime _allSpawnedTime;

        private List<(Vector2 from, Vector2 to, double timer)> _bulletTraces = new();
        public static GameForm Instance { get; private set; }

        public GameForm(bool infiniteMode = false, int selectedMapIndex = 0)
        {
            Instance = this;
            ShouldReturnToMenu = false;
            _infiniteMode = infiniteMode;
            _waveSetCount = 0;
            _enemyHealthMultiplier = 1.0;
            _selectedMapIndex = selectedMapIndex;
            _mapImage1 = SplashKit.LoadBitmap("Map1", @"Resources/map1.jpg");
            _mapImage2 = SplashKit.LoadBitmap("Map2", @"Resources/map2.png");
            _mapImage3 = SplashKit.LoadBitmap("Map3", @"Resources/map3.png");
            _mapImage4 = SplashKit.LoadBitmap("Map4", @"Resources/map4.png");
            _mapImage5 = SplashKit.LoadBitmap("Map5", @"Resources/map5.png");
            _mapImage6 = SplashKit.LoadBitmap("Map6", @"Resources/map6.png");

            switch (_selectedMapIndex)
            {
                case 0: _mapImage = _mapImage1; break;
                case 1: _mapImage = _mapImage2; break;
                case 2: _mapImage = _mapImage3; break;
                case 3: _mapImage = _mapImage4; break;
                case 4: _mapImage = _mapImage5; break;
                case 5: _mapImage = _mapImage6; break;
                default: _mapImage = _mapImage1; break;
            }



            _baseImage = SplashKit.LoadBitmap("Base", @"Resources/base.png");
            SplashKit.LoadSoundEffect("gun", @"Resources/gunsound.wav");
            SplashKit.LoadSoundEffect("laser", @"Resources/lasersound.wav");
            SplashKit.LoadSoundEffect("coin", @"Resources/coinsound.wav");
            _backgroundMusic = SplashKit.LoadMusic("bgm", @"Resources/Delta Force_ Hawk Ops - OST Music Main Theme.wav");
            _backgroundMusic.Play(-1);
            _musicLoaded = true;

            GameManager.Instance.Map.LoadMap(_selectedMapIndex);
            GameManager.Instance.SpawnWave();
        }

        private void HandleWaveProgression()
        {
            if (GameManager.Instance.CurrentWave?.IsCleared() == true)
            {
                if (GameManager.Instance.CurrentWaveNumber < _totalWaves)
                {
                    GameManager.Instance.NextWave();

                    GameManager.Instance.Player.GetType().GetProperty("Money").SetValue(GameManager.Instance.Player, GameManager.Instance.Player.Money + 200);
                    int farmIncome = GameManager.Instance.Map.GetTowers()
                        .OfType<FarmTower>()
                        .Sum(farm => farm.MoneyPerRound);
                    if (farmIncome > 0)
                    {
                        GameManager.Instance.Player.GetType().GetProperty("Money").SetValue(
                            GameManager.Instance.Player,
                            GameManager.Instance.Player.Money + farmIncome
                        );
                    }

                    GameManager.Instance.SpawnWave();
                }
                else if (GameManager.Instance.CurrentWaveNumber >= _totalWaves &&
                         GameManager.Instance.ActiveWaves.Count == 0)
                {
                    if (_infiniteMode)
                    {
                        _waveSetCount++;
                        _enemyHealthMultiplier *= 1.5;
                        GameManager.Instance.NextWave(); // Just increment, do not reset
                        GameManager.Instance.SpawnWave();
                    }
                    else
                    {
                        _endGameState = EndGameState.Victory;
                    }
                }
            }
        }

        private void UpdateSkipPopup()
        {
            if (GameManager.Instance.CurrentWave != null &&
                GameManager.Instance.CurrentWave.AllSpawned &&
                !GameManager.Instance.CurrentWave.IsCleared())
            {
                if (_allSpawnedTime == default)
                    _allSpawnedTime = DateTime.Now;

                if ((DateTime.Now - _allSpawnedTime).TotalSeconds > 3)
                    _showSkipPopup = true;
            }
            else
            {
                _showSkipPopup = false;
                _allSpawnedTime = default;
            }
        }

        private void UpdateTowers(double deltaTime)
        {
            foreach (var tower in GameManager.Instance.Map.GetTowers())
            {
                tower.UpdateCooldown((float)deltaTime);

                var enemiesInRange = GameManager.Instance.ActiveWaves
                    .SelectMany(wave => wave.Enemies)
                    .Where(enemy => Vector2.Distance(tower.Position, enemy.Position) <= tower.Range + enemy.CollisionRadius)
                    .ToList();

                if (enemiesInRange.Any())
                {
                    var target = enemiesInRange.First();
                    if (tower.CanFire)
                    {
                        _bulletTraces.Add((
                            new Vector2((float)(tower.Position.X + 304), (float)(tower.Position.Y + 288)),
                            new Vector2((float)(target.Position.X + 304), (float)(target.Position.Y + 288)),
                            0.1
                        ));
                        if (tower is SplashTower || tower is FreezeTower || tower is PoisonTower || tower is LaserTower)
                            SplashKit.PlaySoundEffect("laser");
                        else
                            SplashKit.PlaySoundEffect("gun");
                    }
                    tower.TryFire(target);
                }
            }
        }

        private void UpdateBulletTraces(double deltaTime)
        {
            for (int i = _bulletTraces.Count - 1; i >= 0; i--)
            {
                var trace = _bulletTraces[i];
                trace.timer -= deltaTime;
                if (trace.timer <= 0)
                    _bulletTraces.RemoveAt(i);
                else
                    _bulletTraces[i] = (trace.from, trace.to, trace.timer);
            }
        }

        private void UpdateFloatingTexts()
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.timer -= 0.016;
                if (ft.timer <= 0)
                    _floatingTexts.RemoveAt(i);
                else
                    _floatingTexts[i] = (ft.pos, ft.text, ft.color, ft.timer);
            }
        }

        private void CheckGameOver()
        {
            if (GameManager.Instance.Player.Health <= 0)
            {
                _endGameState = EndGameState.Defeat;
                GameManager.Instance.EndGame();
            }
        }

        private void HandleRestart()
        {
            GameManager.Instance.StartGame(_selectedMapIndex);
            _isPaused = false;
            _endGameState = EndGameState.None;
            if (_musicLoaded) SplashKit.ResumeMusic();
            ShouldReturnToMenu = false;
            _waveSetCount = 0;
            _enemyHealthMultiplier = 1.0;
        }

        private void HandleHome()
        {
            // Reset the game logic/state
            GameManager.Instance.StartGame(_selectedMapIndex);

            ShouldReturnToMenu = true;
            _isPaused = false;
            _endGameState = EndGameState.None;
            if (_musicLoaded) SplashKit.PauseMusic();
            _waveSetCount = 0;
            _enemyHealthMultiplier = 1.0;

            // Optionally clear UI state
            _selectedTowerType = TowerType.None;
            _selectedTower = null;
            _showSkipPopup = false;
            _allSpawnedTime = default;
            _floatingTexts.Clear();
            _bulletTraces.Clear();
        }



        public void AddFloatingText(Vector2 pos, string text, Color color, double duration = 0.5)
        {
            _floatingTexts.Add((pos, text, color, duration));
        }

       

        private void CheckMoneySound()
        {
            int currentMoney = GameManager.Instance.Player.Money;
            if (_lastMoney != -1 && currentMoney > _lastMoney)
            {
                SplashKit.PlaySoundEffect("coin");
            }
            _lastMoney = currentMoney;
        }

        public void Update()
        {
            if (_endGameState != EndGameState.None || _isPaused) return;
            double deltaTime = 0.016 * GameManager.Instance.GameSpeed;

            foreach (var wave in GameManager.Instance.ActiveWaves.ToList())
            {
                wave.UpdateSpawning(deltaTime);
                wave.RemoveDefeatedEnemies();
                foreach (var enemy in wave.Enemies.ToList())
                    enemy.Move();

                if (wave.IsCleared())
                    GameManager.Instance.ActiveWaves.Remove(wave);
            }

            UpdateSkipPopup();
            HandleWaveProgression();
            UpdateTowers(deltaTime);
            UpdateBulletTraces(deltaTime);
            CheckGameOver();
            UpdateFloatingTexts();
            CheckMoneySound();
        }

        public void HandleMouseInput()
        {
            bool mouseClicked = SplashKit.MouseClicked(MouseButton.LeftButton);
            Point2D mouse = SplashKit.MousePosition();

            if (_endGameState != EndGameState.None)
            {
                Rectangle restartBtn = new Rectangle() { X = 518, Y = 550, Width = 500, Height = 100 };
                Rectangle homeBtn = new Rectangle() { X = 518, Y = 700, Width = 500, Height = 100 };

                if (mouseClicked)
                {
                    if (SplashKit.PointInRectangle(mouse, restartBtn))
                    {
                        HandleRestart();
                        return;
                    }
                    else if (SplashKit.PointInRectangle(mouse, homeBtn))
                    {
                        HandleHome();
                        return;
                    }
                }
                return; // Block other input
            }



            // Speed button click
            if (mouseClicked)
            {
                if (SplashKit.PointInRectangle(mouse, _speedButtonRect))
                {
                    GameManager.Instance.ToggleGameSpeed();
                    return;
                }
            }

            if (_isPaused)
            {
                if (_musicLoaded) SplashKit.PauseMusic();
                if (mouseClicked)
                {
                    if (mouseClicked)
                    {
                        if (SplashKit.PointInRectangle(mouse, _resumeButtonRect))
                        {
                            _isPaused = false;
                            if (_musicLoaded) SplashKit.ResumeMusic();
                            return;
                        }
                        else if (SplashKit.PointInRectangle(mouse, _restartButtonRect))
                        {
                            HandleRestart();
                            return;
                        }
                        else if (SplashKit.PointInRectangle(mouse, _homeButtonRect))
                        {
                            HandleHome();
                            return;
                        }
                    }


                }
                return; // Don't process other input while paused
            }

            // Pause button click
            if (mouseClicked)
            {
                if (SplashKit.PointInRectangle(mouse, _pauseButtonRect))
                {
                    _isPaused = true;
                    if (_musicLoaded) SplashKit.PauseMusic();
                    return;
                }
            }

            if (_showSkipPopup && mouseClicked)
            {
                if (SplashKit.PointInRectangle(mouse, _skipPopupRect))
                {
                    // Prevent skipping if already at the last wave
                    if (!_infiniteMode && GameManager.Instance.CurrentWaveNumber >= _totalWaves)
                    {
                        _showSkipPopup = false;
                        _allSpawnedTime = default;
                        return;
                    }


                    GameManager.Instance.NextWave();

                    GameManager.Instance.Player.GetType().GetProperty("Money").SetValue(
                        GameManager.Instance.Player,
                        GameManager.Instance.Player.Money + 200
                    );
                    int farmIncome = GameManager.Instance.Map.GetTowers()
                        .OfType<FarmTower>()
                        .Sum(farm => farm.MoneyPerRound);
                    if (farmIncome > 0)
                    {
                        GameManager.Instance.Player.GetType().GetProperty("Money").SetValue(
                            GameManager.Instance.Player,
                            GameManager.Instance.Player.Money + farmIncome
                        );
                        // Show floating text above each farm
                        foreach (var farm in GameManager.Instance.Map.GetTowers().OfType<FarmTower>())
                        {
                            GameForm.Instance?.AddFloatingText(farm.Position, $"+${farm.MoneyPerRound}", SplashKitSDK.Color.Yellow);
                        }
                    }


                    GameManager.Instance.SpawnWave();
                    _showSkipPopup = false;
                    _allSpawnedTime = default;
                    return;
                }
            }





            if (mouseClicked)
            {

                // 1. Check if clicked on a tower (select tower)
                foreach (var tower in GameManager.Instance.Map.GetTowers())
                {
                    double dx = mouse.X - (tower.Position.X + 304); // Adjust for offset
                    double dy = mouse.Y - (tower.Position.Y + 288);
                    if (Math.Sqrt(dx * dx + dy * dy) <= 20) // 20 is half the tower sprite size
                    {
                        _selectedTower = tower;
                        return;
                    }
                }

                // 2. If a tower is selected, check for upgrade/sell button clicks
                if (_selectedTower != null)
                {
                    // Upgrade button area
                    if (!_selectedTower.IsMaxed && mouse.X >= 20 && mouse.X <= 140 && mouse.Y >= 250 && mouse.Y <= 290)
                    {
                        GameManager.Instance.Player.UpgradeTower(_selectedTower);
                        return;
                    }
                    // Sell button area
                    else if (mouse.X >= 160 && mouse.X <= 280 && mouse.Y >= 250 && mouse.Y <= 290)
                    {
                        // Refund half the cost
                        GameManager.Instance.Player.GetType().GetProperty("Money").SetValue(
                            GameManager.Instance.Player,
                            GameManager.Instance.Player.Money + _selectedTower.Cost / 2
                        );
                        GameManager.Instance.Map.GetTowers().Remove(_selectedTower);
                        _selectedTower = null;
                        return;
                    }
                }

                // 3. Check if clicked on a tower button
                if (SplashKit.PointInRectangle(mouse, _gunnerRect)) { _selectedTowerType = TowerType.Gunner; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _shotgunnerRect)) { _selectedTowerType = TowerType.Shotgunner; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _sniperRect)) { _selectedTowerType = TowerType.Sniper; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _farmRect)) { _selectedTowerType = TowerType.Farm; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _splashRect)) { _selectedTowerType = TowerType.Splash; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _freezeRect)) { _selectedTowerType = TowerType.Freeze; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _poisonRect)) { _selectedTowerType = TowerType.Poison; _selectedTower = null; return; }
                else if (SplashKit.PointInRectangle(mouse, _laserRect)) { _selectedTowerType = TowerType.Laser; _selectedTower = null; return; }



                // 4. If clicked elsewhere on the map and a tower is selected, try to place it
                if (_selectedTowerType != TowerType.None && mouse.Y < 1152 - 100)
                {
                    // Count existing towers by type
                    var towers = GameManager.Instance.Map.GetTowers();
                    int farmCount = towers.Count(t => t is FarmTower);
                    int gunnerCount = towers.Count(t => t is GunnerTower);
                    int shotgunnerCount = towers.Count(t => t is ShotgunnerTower);
                    int sniperCount = towers.Count(t => t is SniperTower);
                    int splashCount = towers.Count(t => t is SplashTower);
                    int freezeCount = towers.Count(t => t is FreezeTower);
                    int poisonCount = towers.Count(t => t is PoisonTower);
                    int laserCount = towers.Count(t => t is LaserTower);

                    // Enforce limits (max 8 for all except Farm, which is 4)
                    if ((_selectedTowerType == TowerType.Farm && farmCount >= 4) ||
                        (_selectedTowerType == TowerType.Gunner && gunnerCount >= 8) ||
                        (_selectedTowerType == TowerType.Shotgunner && shotgunnerCount >= 8) ||
                        (_selectedTowerType == TowerType.Sniper && sniperCount >= 8) ||
                        (_selectedTowerType == TowerType.Splash && splashCount >= 8) ||
                        (_selectedTowerType == TowerType.Freeze && freezeCount >= 8) ||
                        (_selectedTowerType == TowerType.Poison && poisonCount >= 8) ||
                        (_selectedTowerType == TowerType.Laser && laserCount >= 8))
                    {
                        _selectedTowerType = TowerType.None;
                        _selectedTower = null;
                        return;
                    }


                    Vector2 pos = new Vector2((float)mouse.X - 304, (float)mouse.Y - 288);
                    Tower tower = null;
                    switch (_selectedTowerType)
                    {
                        case TowerType.Gunner: tower = new GunnerTower(); break;
                        case TowerType.Shotgunner: tower = new ShotgunnerTower(); break;
                        case TowerType.Sniper: tower = new SniperTower(); break;
                        case TowerType.Farm: tower = new FarmTower(); break;
                        case TowerType.Splash: tower = new SplashTower(); break;
                        case TowerType.Freeze: tower = new FreezeTower(); break;
                        case TowerType.Poison: tower = new PoisonTower(); break;
                        case TowerType.Laser: tower = new LaserTower(); break;
                    }

                    if (tower != null)
                    {
                        GameManager.Instance.Player.PlaceTower(tower, pos);
                        _selectedTowerType = TowerType.None; // Deselect after placing
                        _selectedTower = null; // Deselect tower after placing
                    }
                }

                if (_selectedTower != null)
                {
                    // Deselect the tower if the click was not on upgrade/sell buttons or a tower
                    _selectedTower = null;
                }
            }
        }



        private readonly object _bulletTracesLock = new();

        public void GameLoopTick(object state)
        {
            Update();
        }






        public void Draw()
        {
            SplashKit.ClearScreen(Color.White);
            double scale = 2.0;
            var opts = SplashKit.OptionScaleBmp(scale, scale);
            SplashKit.DrawBitmap(_mapImage, 384, 288, opts);



            // Draw GUI background bar
            SplashKit.FillRectangle(Color.Black, 0, 0, 1536, 50);
            Point2D mouse = SplashKit.MousePosition();
            Color pauseBtnColor = SplashKit.PointInRectangle(mouse, _pauseButtonRect) ? Color.Yellow : Color.LightGray;
            SplashKit.FillRectangle(pauseBtnColor, _pauseButtonRect);
            SplashKit.DrawText("II", Color.Black, "Arial", 28, _pauseButtonRect.X + 8, _pauseButtonRect.Y + 2);
            // Draw speed button
            string speedText;
            Color speedBtnColor;

            if (GameManager.Instance.GameSpeed == 0.5)
            {
                // 2x speed: show "1x" (click to go slower), green
                speedText = "1x";
                speedBtnColor = SplashKit.PointInRectangle(mouse, _speedButtonRect) ? Color.Yellow : Color.LightGray;
            }
            else
            {
                // Normal speed: show "2x" (click to go faster), gray
                speedText = "2x";
                speedBtnColor = SplashKit.PointInRectangle(mouse, _speedButtonRect) ? Color.Yellow : Color.Green;
            }

            SplashKit.FillRectangle(speedBtnColor, _speedButtonRect);
            SplashKit.DrawText(speedText, Color.Black, "Arial", 22, _speedButtonRect.X + 4, _speedButtonRect.Y + 8);


            // Prepare wave/enemy text
            int enemiesLeft = GameManager.Instance.ActiveWaves.Sum(wave => wave.Enemies.Count);
            string waveText;
            if (_infiniteMode)
                waveText = $"Wave: {GameManager.Instance.CurrentWaveNumber}/inf";
            else
                waveText = $"Wave: {GameManager.Instance.CurrentWaveNumber}/{_totalWaves}";


            string enemyText = $"Enemies Left: {enemiesLeft}";
            string healthText = $"Health: {GameManager.Instance.Player.Health}";
            string moneyText = $"Money: {GameManager.Instance.Player.Money}";
            // Draw wave and enemy count
            SplashKit.DrawText(waveText, Color.White, "Arial", 24, 20, 10);
            SplashKit.DrawText(enemyText, Color.White, "Arial", 24, 220, 10);
            SplashKit.DrawText(healthText, Color.White, "Arial", 24, 420, 10);
            SplashKit.DrawText(moneyText, Color.White, "Arial", 24, 620, 10);

                string infText = $"Set: {_waveSetCount + 1}  Enemy HP x{_enemyHealthMultiplier:F2}";
                SplashKit.DrawText(infText, Color.Yellow, "Arial", 24, 900, 10);
            

            double mapOffsetX = 384;
            double mapOffsetY = 288;
            double cellSize = 40;
            var testBitmap = SplashKit.LoadBitmap("TestNormal", @"Resources/normal.jpg");

            var thickLineOpts = SplashKit.OptionLineWidth(6);
            if (_selectedMapIndex == 0)
            {
                // Map 1: Oval
                double ovalRadiusX = 400;
                double ovalRadiusY = 240;
                Vector2 center = new Vector2(688, 596);
                int steps = 200;
                List<Vector2> pathPoints = new List<Vector2>();
                for (int i = 0; i <= steps; i++)
                {
                    double angle = (2 * Math.PI * i) / steps;
                    float x = (float)(center.X + ovalRadiusX * Math.Cos(angle));
                    float y = (float)(center.Y + ovalRadiusY * Math.Sin(angle));
                    pathPoints.Add(new Vector2(x, y));
                }
                for (int i = 1; i < pathPoints.Count; i++)
                    SplashKit.DrawLine(Color.Green, pathPoints[i - 1].X, pathPoints[i - 1].Y, pathPoints[i].X, pathPoints[i].Y, thickLineOpts);
            }
            else
            {
                Color[] pathColors = { Color.Blue, Color.Purple, Color.Orange, Color.Red, Color.YellowGreen };
                int colorIdx = Math.Max(0, Math.Min(_selectedMapIndex - 1, pathColors.Length - 1));
                var pathPoints = GameManager.Instance.Map.Path;
                for (int i = 1; i < pathPoints.Count; i++)
                    SplashKit.DrawLine(
                        pathColors[colorIdx],
                        pathPoints[i - 1].X + 304, pathPoints[i - 1].Y + 288,
                        pathPoints[i].X + 304, pathPoints[i].Y + 288,
                        thickLineOpts);
            }



            if (_isPaused)
            {
                // Center the buttons
                int centerX = 1536 / 2;
                int centerY = 1152 / 2;
                _resumeButtonRect = new Rectangle() { X = centerX - 100, Y = centerY - 110, Width = 200, Height = 60 };
                _restartButtonRect = new Rectangle() { X = centerX - 100, Y = centerY - 30, Width = 200, Height = 60 };
                _homeButtonRect = new Rectangle() { X = centerX - 100, Y = centerY + 50, Width = 200, Height = 60 };

                // Resume button
                bool isResumeHover = SplashKit.PointInRectangle(mouse, _resumeButtonRect);
                Color resumeBtnColor = isResumeHover ? Color.Yellow : Color.LightGray;
                Color resumeBorderColor = isResumeHover ? Color.Orange : Color.Black;
                SplashKit.FillRectangle(resumeBtnColor, _resumeButtonRect);
                SplashKit.DrawRectangle(resumeBorderColor, _resumeButtonRect);
                SplashKit.DrawText("Resume", Color.Black, "Arial", 28, _resumeButtonRect.X + 40, _resumeButtonRect.Y + 15);

                // Restart button
                bool isRestartHover = SplashKit.PointInRectangle(mouse, _restartButtonRect);
                Color restartBtnColor = isRestartHover ? Color.Yellow : Color.LightGray;
                Color restartBorderColor = isRestartHover ? Color.Orange : Color.Black;
                SplashKit.FillRectangle(restartBtnColor, _restartButtonRect);
                SplashKit.DrawRectangle(restartBorderColor, _restartButtonRect);
                SplashKit.DrawText("Restart", Color.Black, "Arial", 28, _restartButtonRect.X + 40, _restartButtonRect.Y + 15);

                // Home button
                bool isHomeHover = SplashKit.PointInRectangle(mouse, _homeButtonRect);
                Color homeBtnColor = isHomeHover ? Color.Yellow : Color.LightGray;
                Color homeBorderColor = isHomeHover ? Color.Orange : Color.Black;
                SplashKit.FillRectangle(homeBtnColor, _homeButtonRect);
                SplashKit.DrawRectangle(homeBorderColor, _homeButtonRect);
                SplashKit.DrawText("Home", Color.Black, "Arial", 28, _homeButtonRect.X + 60, _homeButtonRect.Y + 15);

                SplashKit.RefreshScreen();
                return;
            }



            // Draw all placed towers
            foreach (var tower in GameManager.Instance.Map.GetTowers())
            {
                // Draw tower radius ONLY for the selected tower
                if (_selectedTower == tower)
                {
                    SplashKit.FillCircle(
                        SplashKit.RGBAColor(0, 0, 255, 60), // semi-transparent blue
                        tower.Position.X + 304,
                        tower.Position.Y + 288,
                        tower.Range
                    );
                }



                // Draw tower sprite, centered
                if (tower.Sprite != null)
                {
                    double towerScale = 40.0 / tower.Sprite.Width;
                    var towerOpts = SplashKit.OptionScaleBmp(towerScale, towerScale);
                    SplashKit.DrawBitmap(tower.Sprite, tower.Position.X - 20, tower.Position.Y - 20, towerOpts);
                }
            }


            // Draw bullet traces with custom colors for each tower type
            foreach (var trace in _bulletTraces.ToList())
            {
                // Find the tower that fired this trace (by matching position)
                var tower = GameManager.Instance.Map.GetTowers()
                    .FirstOrDefault(t =>
                        Math.Abs((t.Position.X + 304) - trace.from.X) < 0.1 &&
                        Math.Abs((t.Position.Y + 288) - trace.from.Y) < 0.1);

                Color traceColor = Color.Orange; // default

                if (tower is SplashTower) traceColor = Color.Blue;
                else if (tower is FreezeTower) traceColor = Color.Cyan;
                else if (tower is PoisonTower) traceColor = Color.Green;
                else if (tower is LaserTower) traceColor = Color.Red;

                SplashKit.DrawLine(traceColor, trace.from.X, trace.from.Y, trace.to.X, trace.to.Y, SplashKit.OptionLineWidth(4));
            }


            foreach (var wave in GameManager.Instance.ActiveWaves.ToList())
            {
                // Fixed position for enemy info text
                Vector2 infoTextPosition = new Vector2(688, 596);

                foreach (var enemy in wave.Enemies.ToList())
                {
                    double x = enemy.Position.X;
                    double y = enemy.Position.Y;
                    var enemyOpts = SplashKit.OptionScaleBmp(40.0 / enemy.Sprite.Width, 40.0 / enemy.Sprite.Height);
                    SplashKit.DrawBitmap(enemy.Sprite, x - 20, y - 20, enemyOpts);

                    // Draw enemy type and health above the enemy as it moves
                    string infoText = $"{enemy.GetType().Name} | HP: {enemy.Health}";
                    int fontSize = 16;
                    double textWidth = SplashKit.TextWidth(infoText, "Arial", fontSize);

                    double offsetX = 688 - 384;
                    double offsetY = 596 - 288;

                    SplashKit.DrawText(
                        infoText,
                        Color.Black,
                        "Arial",
                        fontSize,
                        x + offsetX - textWidth / 2,
                        y + offsetY - 35
                    
                    );

                    // Draw the enemy's collision radius as a semi-transparent red circle
                    SplashKit.FillCircle(
                        SplashKit.RGBAColor(255, 0, 0, 80), // semi-transparent red
                        (float)(x + 304),
                        (float)(y + 288),
                        (float)enemy.CollisionRadius
                    );


                }


                // Only access [0] if there is at least one enemy
                if (GameManager.Instance.CurrentWave.Enemies.Count > 0)
                {
                    var enemyPos = GameManager.Instance.CurrentWave.Enemies[0].Position;
                    // ...
                

                string posText = $"Enemy position: ({enemyPos.X:F2}, {enemyPos.Y:F2})";
                SplashKit.DrawText(posText, Color.Black, 20, 20);
                }
            }


            // Draw bottom GUI bar
            int guiBarHeight = 100;
            SplashKit.FillRectangle(Color.DarkGray, 0, 1152 - guiBarHeight, 1536, guiBarHeight);

            // Define rectangles for tower icons/buttons
            _gunnerRect = new Rectangle() { X = 100, Y = 1152 - 90, Width = 80, Height = 80 };
            _shotgunnerRect = new Rectangle() { X = 200, Y = 1152 - 90, Width = 80, Height = 80 };
            _sniperRect = new Rectangle() { X = 300, Y = 1152 - 90, Width = 80, Height = 80 };
            _farmRect = new Rectangle() { X = 400, Y = 1152 - 90, Width = 80, Height = 80 };
            _splashRect = new Rectangle() { X = 500, Y = 1152 - 90, Width = 80, Height = 80 };
            _freezeRect = new Rectangle() { X = 600, Y = 1152 - 90, Width = 80, Height = 80 };
            _poisonRect = new Rectangle() { X = 700, Y = 1152 - 90, Width = 80, Height = 80 };
            _laserRect = new Rectangle() { X = 800, Y = 1152 - 90, Width = 80, Height = 80 };

            // Draw improved tower selection buttons with icon, price, and name
            var towerData = new[]
            {
                (rect: _gunnerRect, name: "Gunner", price: 120, type: TowerType.Gunner),
                (rect: _shotgunnerRect, name: "Shotgunner", price: 140, type: TowerType.Shotgunner),
                (rect: _sniperRect, name: "Sniper", price: 160, type: TowerType.Sniper),
                (rect: _farmRect, name: "Farm", price: 150, type: TowerType.Farm),
                (rect: _splashRect, name: "Splash", price: 150, type: TowerType.Splash),
                (rect: _freezeRect, name: "Freeze", price: 120, type: TowerType.Freeze),
                (rect: _poisonRect, name: "Poison", price: 110, type: TowerType.Poison),
                (rect: _laserRect, name: "Laser", price: 160, type: TowerType.Laser)
            };
            // Count towers by type
            var towers = GameManager.Instance.Map.GetTowers();
            int gunnerCount = towers.Count(t => t is GunnerTower);
            int shotgunnerCount = towers.Count(t => t is ShotgunnerTower);
            int sniperCount = towers.Count(t => t is SniperTower);
            int farmCount = towers.Count(t => t is FarmTower);
            int splashCount = towers.Count(t => t is SplashTower);
            int freezeCount = towers.Count(t => t is FreezeTower);
            int poisonCount = towers.Count(t => t is PoisonTower);
            int laserCount = towers.Count(t => t is LaserTower);

            foreach (var t in towerData)
            {
                // Highlight if selected
                Color towerColor;
                if (SplashKit.PointInRectangle(mouse, t.rect))
                    towerColor = Color.Yellow;  // highlight on hover
                else if (_selectedTowerType == t.type)
                    towerColor = Color.Green;   // selected tower
                else
                    towerColor = Color.LightGray;  // normal

                SplashKit.FillRectangle(towerColor, t.rect);

                // Draw price on top in black
                string priceText = $"${t.price}";
                SplashKit.DrawText(priceText, Color.Black, "Arial", 18, t.rect.X + (t.rect.Width - SplashKit.TextWidth(priceText, "Arial", 18)) / 2, t.rect.Y + 2);

                // Draw name centered vertically in the button
                SplashKit.DrawText(t.name, Color.Black, "Arial", 16, t.rect.X + (t.rect.Width - SplashKit.TextWidth(t.name, "Arial", 16)) / 2, t.rect.Y + t.rect.Height / 2 - 8);

                // Draw count under the name
                int count = 0, max = 8;
                switch (t.type)
                {
                    case TowerType.Gunner: count = gunnerCount; max = 8; break;
                    case TowerType.Shotgunner: count = shotgunnerCount; max = 8; break;
                    case TowerType.Sniper: count = sniperCount; max = 8; break;
                    case TowerType.Farm: count = farmCount; max = 4; break;
                    case TowerType.Splash: count = splashCount; max = 8; break;
                    case TowerType.Freeze: count = freezeCount; max = 8; break;
                    case TowerType.Poison: count = poisonCount; max = 8; break;
                    case TowerType.Laser: count = laserCount; max = 8; break;
                }
                string countText = $"{count}/{max}";
                SplashKit.DrawText(countText, Color.Black, "Arial", 14, t.rect.X + (t.rect.Width - SplashKit.TextWidth(countText, "Arial", 14)) / 2, t.rect.Y + t.rect.Height - 22);
            }






            if (_showSkipPopup)
            {
                // Draw a shadow for depth
                SplashKit.FillRectangle(SplashKit.RGBAColor(0, 0, 0, 60),
                    _skipPopupRect.X + 4, _skipPopupRect.Y + 4, _skipPopupRect.Width, _skipPopupRect.Height);

                // Change color on hover
                bool isHover = SplashKit.PointInRectangle(SplashKit.MousePosition(), _skipPopupRect);
                Color buttonColor = isHover ? Color.Yellow : Color.LightGray;
                Color borderColor = isHover ? Color.Orange : Color.Black;

                // Draw button
                SplashKit.FillRectangle(buttonColor, _skipPopupRect);
                SplashKit.DrawRectangle(borderColor, _skipPopupRect);

                // Draw text centered
                string btnText = "Skip to Next Wave";
                int fontSize = 22;
                double textWidth = SplashKit.TextWidth(btnText, "Arial", fontSize);
                double textX = _skipPopupRect.X + (_skipPopupRect.Width - textWidth) / 2;
                double textY = _skipPopupRect.Y + (_skipPopupRect.Height - fontSize) / 2;
                SplashKit.DrawText(btnText, Color.Black, "Arial", fontSize, textX, textY);
            }

            foreach (var ft in _floatingTexts.ToList())
            {
                SplashKit.DrawText(
                    ft.text,
                    ft.color,
                    "Arial",
                    20,
                    ft.pos.X + 304 - SplashKit.TextWidth(ft.text, "Arial", 20) / 2,
                    ft.pos.Y + 288 - 40 // 40 pixels above the entity
                );
            }

            if (_endGameState != EndGameState.None)
            {
                // ... (existing code for dim background and big text)

                Rectangle restartBtn = new Rectangle() { X = 518, Y = 550, Width = 500, Height = 100 };
                Rectangle homeBtn = new Rectangle() { X = 518, Y = 700, Width = 500, Height = 100 };

                // Restart button
                bool isRestartHover = SplashKit.PointInRectangle(mouse, restartBtn);
                Color restartBtnColor = isRestartHover ? Color.Yellow : Color.LightGray;
                Color restartBorderColor = isRestartHover ? Color.Orange : Color.Black;
                SplashKit.FillRectangle(restartBtnColor, restartBtn);
                SplashKit.DrawRectangle(restartBorderColor, restartBtn);
                SplashKit.DrawText("Restart", Color.Black, "Arial", 48, restartBtn.X + 150, restartBtn.Y + 25);

                // Home button
                bool isHomeHover = SplashKit.PointInRectangle(mouse, homeBtn);
                Color homeBtnColor = isHomeHover ? Color.Yellow : Color.LightGray;
                Color homeBorderColor = isHomeHover ? Color.Orange : Color.Black;
                SplashKit.FillRectangle(homeBtnColor, homeBtn);
                SplashKit.DrawRectangle(homeBorderColor, homeBtn);
                SplashKit.DrawText("Home", Color.Black, "Arial", 48, homeBtn.X + 150, homeBtn.Y + 25);

                SplashKit.RefreshScreen();
                return;
            }

            // Draw selected tower UI panel LAST so it is always on top
            if (_selectedTower != null)
            {
                SplashKit.FillRectangle(Color.LightGray, 0, 60, 300, 300);
                SplashKit.DrawText("TOWER STATS", Color.Black, "Arial", 20, 20, 70);
                SplashKit.DrawText($"Type: {_selectedTower.GetType().Name}", Color.Black, "Arial", 16, 20, 100);
                SplashKit.DrawText($"Level: {_selectedTower.UpgradeLevel}/{_selectedTower.MaxUpgradeLevel}", Color.Black, "Arial", 16, 20, 130);
                SplashKit.DrawText($"Damage: {_selectedTower.Damage}", Color.Black, "Arial", 16, 20, 160);
                SplashKit.DrawText($"Range: {_selectedTower.Range}", Color.Black, "Arial", 16, 20, 190);
                SplashKit.DrawText($"Cooldown: {_selectedTower.Cooldown:F2}", Color.Black, "Arial", 16, 20, 220);

                if (!_selectedTower.IsMaxed)
                {
                    bool isUpgradeHover = mouse.X >= 20 && mouse.X <= 140 && mouse.Y >= 250 && mouse.Y <= 290;
                    Color upgradeBtnColor = isUpgradeHover ? Color.Yellow : Color.LightGray;
                    Color upgradeBorderColor = isUpgradeHover ? Color.Orange : Color.Black;
                    SplashKit.FillRectangle(upgradeBtnColor, 20, 250, 120, 40);
                    SplashKit.DrawRectangle(upgradeBorderColor, 20, 250, 120, 40);
                    SplashKit.DrawText($"Upgrade (${_selectedTower.UpgradeCost})", Color.Black, "Arial", 16, 30, 260);
                }
                else
                {
                    SplashKit.DrawText("Maxxed", Color.Red, "Arial", 20, 20, 250);
                }

                bool isSellHover = mouse.X >= 160 && mouse.X <= 280 && mouse.Y >= 250 && mouse.Y <= 290;
                Color sellBtnColor = isSellHover ? Color.Yellow : Color.DarkRed;
                Color sellBorderColor = isSellHover ? Color.Orange : Color.Black;
                SplashKit.FillRectangle(sellBtnColor, 160, 250, 120, 40);
                SplashKit.DrawRectangle(sellBorderColor, 160, 250, 120, 40);
                SplashKit.DrawText("Sell", Color.Black, "Arial", 16, 200, 260);
            }

            SplashKit.RefreshScreen();
        }
    }


}


