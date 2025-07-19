using System;
using System.Collections.Generic;
using System.Numerics;

namespace ShapeDrawer.lib
{
    // Singleton GameManager
    internal class GameManager
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance ??= new GameManager();

        private List<IObserver> observers = new List<IObserver>();

        public int _currentWaveNumber = 1;
        public int CurrentWaveNumber => _currentWaveNumber;
        private double _gameSpeed = 0.5;
        public double GameSpeed => _gameSpeed;
        public void ToggleGameSpeed()
        {
            _gameSpeed = (_gameSpeed == 0.5) ? 1.0 : 0.5;
            // Optionally notify observers/UI
        }
        public void NextWave()
        {
            _currentWaveNumber++;
        }
        public List<Wave> ActiveWaves { get; } = new List<Wave>();
        public Player Player { get; private set; }
        public Map Map { get; private set; }
        public Wave CurrentWave { get; set; }

        private GameManager()
        {
            Player = new Player();
            Map = new Map();
        }

        public void RegisterObserver(IObserver observer)
        {
            observers.Add(observer);
        }

        public void UnregisterObserver(IObserver observer)
        {
            observers.Remove(observer);
        }

        private void NotifyObservers(string eventType, object data)
        {
            foreach (var observer in observers)
            {
                observer.Update(eventType, data);
            }
        }

        public void StartGame(int mapIndex)
        {
            // Clear all towers, enemies, and waves
            Map.GetTowers().Clear();
            ActiveWaves.Clear();
            _currentWaveNumber = 1;
            CurrentWave = null;

            // Reset player stats (use reflection to set private setters)
            typeof(Player).GetProperty("Health")!.SetValue(Player, 100);
            typeof(Player).GetProperty("Money")!.SetValue(Player, 500);

            // Reload map with the correct index
            Map.LoadMap(mapIndex);

            // Start first wave
            SpawnWave();
        }





        public void EndGame()
        {
            Console.WriteLine("Game Over!");
            NotifyObservers("GameOver", null);
        }

        public void SpawnWave()
        {
            List<(Type, int)> wavePlan = new List<(Type, int)>();
            switch (_currentWaveNumber)
            {
                case 1:
                    wavePlan.Add((typeof(NormalEnemy), 5));
                    break;
                case 2:
                    wavePlan.Add((typeof(NormalEnemy), 8));
                    break;
                case 3:
                    wavePlan.Add((typeof(NormalEnemy), 10));
                    wavePlan.Add((typeof(AirEnemy), 2));
                    break;
                case 4:
                    wavePlan.Add((typeof(NormalEnemy), 12));
                    wavePlan.Add((typeof(AirEnemy), 3));
                    break;
                case 5:
                    wavePlan.Add((typeof(NormalEnemy), 10));
                    wavePlan.Add((typeof(AirEnemy), 5));
                    wavePlan.Add((typeof(BossEnemy), 1));
                    break;
                case 6:
                    wavePlan.Add((typeof(NormalEnemy), 14));
                    wavePlan.Add((typeof(AirEnemy), 6));
                    break;
                case 7:
                    wavePlan.Add((typeof(NormalEnemy), 10));
                    wavePlan.Add((typeof(AirEnemy), 8));
                    wavePlan.Add((typeof(BossEnemy), 1));
                    break;
                case 8:
                    wavePlan.Add((typeof(NormalEnemy), 16));
                    wavePlan.Add((typeof(AirEnemy), 10));
                    break;
                case 9:
                    wavePlan.Add((typeof(NormalEnemy), 12));
                    wavePlan.Add((typeof(AirEnemy), 10));
                    wavePlan.Add((typeof(BossEnemy), 2));
                    break;
                case 10:
                    wavePlan.Add((typeof(NormalEnemy), 18));
                    wavePlan.Add((typeof(AirEnemy), 12));
                    break;
                case 11:
                    wavePlan.Add((typeof(NormalEnemy), 15));
                    wavePlan.Add((typeof(AirEnemy), 12));
                    wavePlan.Add((typeof(BossEnemy), 2));
                    break;
                case 12:
                    wavePlan.Add((typeof(NormalEnemy), 20));
                    wavePlan.Add((typeof(AirEnemy), 14));
                    break;
                case 13:
                    wavePlan.Add((typeof(NormalEnemy), 15));
                    wavePlan.Add((typeof(AirEnemy), 15));
                    wavePlan.Add((typeof(BossEnemy), 3));
                    break;
                case 14:
                    wavePlan.Add((typeof(NormalEnemy), 22));
                    wavePlan.Add((typeof(AirEnemy), 16));
                    break;
                case 15:
                    wavePlan.Add((typeof(NormalEnemy), 10));
                    wavePlan.Add((typeof(AirEnemy), 10));
                    wavePlan.Add((typeof(BossEnemy), 5)); // Final boss wave
                    break;
                default:
                    // Optionally, repeat or end game
                    wavePlan.Add((typeof(BossEnemy), 10));
                    break;
            }
            var wave = new Wave(wavePlan);
            ActiveWaves.Add(wave);
            CurrentWave = wave;
            NotifyObservers("WaveStarted", wave);
        }


    }

    // Player class
    internal class Player
    {
        public int Health { get; private set; } = 100;
        public int Money { get; private set; } = 500;

        public void PlaceTower(Tower tower, Vector2 position)
        {
            if (Money >= tower.Cost && GameManager.Instance.Map.CanPlaceTower(position))
            {
                Money -= tower.Cost;
                GameManager.Instance.Map.AddTower(tower, position);
            }
            else if (Money < tower.Cost)
            {
                Console.WriteLine("Not enough money to place this tower.");
            }
        }


        public void UpgradeTower(Tower tower)
        {
            if (Money >= tower.UpgradeCost)
            {
                Money -= tower.UpgradeCost;
                tower.Upgrade();
                Console.WriteLine("Tower upgraded successfully!");
            }
            else
            {
                Console.WriteLine("Not enough money to upgrade the tower.");
            }
        }
    }


    // Wave class
    internal class Wave
    {
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

        // Accepts a list of (type, count) pairs
        public Wave(List<(Type enemyType, int count)> enemyPlan)
        {
            Enemies.Clear();
            _spawnQueue.Clear();
            foreach (var (enemyType, count) in enemyPlan)
                for (int i = 0; i < count; i++)
                    _spawnQueue.Enqueue(enemyType);
            _spawnTimer = 0;
            AllSpawned = false;
            Console.WriteLine("Wave ready to spawn.");
        }

        public void UpdateSpawning(double deltaTime)
        {
            if (AllSpawned) return;
            _spawnTimer += deltaTime * GameManager.Instance.GameSpeed;
            if (_spawnTimer >= _spawnInterval && _spawnQueue.Count > 0)
            {
                var enemyType = _spawnQueue.Dequeue();
                var enemy = (Enemy)Activator.CreateInstance(enemyType);
                // Apply health multiplier if in infinite mode
                if (GameForm.Instance != null && GameForm.Instance._infiniteMode)
                {
                    enemy.Health = (int)(enemy.Health * GameForm.Instance._enemyHealthMultiplier);
                }

                var path = GameManager.Instance.Map.Path;
                if (path == null || path.Count == 0)
                {
                    Console.WriteLine("Error: Map path is not initialized or empty.");
                    return;
                }
                enemy.Position = path[0];
                enemy.PathIndex = 0;
                Enemies.Add(enemy);
                enemy.Position = GameManager.Instance.Map.Path[0];
                enemy.PathIndex = 0;
                Enemies.Add(enemy);
                _spawnTimer = 0;
                if (_spawnQueue.Count == 0)
                    AllSpawned = true;
            }
        }


        public bool IsCleared() => AllSpawned && Enemies.Count == 0;
        public void RemoveDefeatedEnemies() => Enemies.RemoveAll(enemy => enemy.Health <= 0);
        private Queue<Type> _spawnQueue = new Queue<Type>();
        private double _spawnTimer = 0;
        private double _spawnInterval = 0.5; // seconds between spawns
        public bool AllSpawned { get; private set; } = false;

    }


    // Enemy class
    internal class Enemy
    {
        public double CollisionRadius => 20.0;
        public int Health { get; set; } // Allow derived classes to set Health
        public float Speed { get; set; } // Allow derived classes to set Speed
        public Vector2 Position { get; set; } = new Vector2(0, 0); // Default Position
        public List<Vector2> ScreenPath { get; set; }
        public SplashKitSDK.Bitmap Sprite { get; set; }
        public int PathIndex { get; set; } = 0;

        // Oval path movement fields
        private double _ovalAngle = 0;
        private const double _ovalAngleStep = 0.01; // Lower = slower, higher = faster
        private const double _ovalRadiusX = 400;    // Horizontal radius
        private const double _ovalRadiusY = 240;    // Vertical radius (0.6 * 400)
        private Vector2 _ovalCenter = new Vector2(384, 288); // Center of the oval (adjust as needed)
        private bool _finishedOval = false;

        public virtual void Move()
        {
            int mapId = GameForm.Instance != null ? GameForm.Instance._selectedMapIndex : 0;
            if (mapId == 0)
            {
                // Original oval movement for map 0
                if (_finishedOval) return;

                _ovalAngle += _ovalAngleStep * Speed * GameManager.Instance.GameSpeed;

                if (_ovalAngle >= 2 * Math.PI)
                {
                    _ovalAngle = 2 * Math.PI;
                    _finishedOval = true;

                    GameManager.Instance.Player.GetType().GetProperty("Health").SetValue(GameManager.Instance.Player, GameManager.Instance.Player.Health - 5);
                    Health = 0;
                    GameManager.Instance.CurrentWave?.RemoveDefeatedEnemies();
                    return;
                }

                Position = new Vector2(
                    _ovalCenter.X + (float)(_ovalRadiusX * Math.Cos(_ovalAngle)),
                    _ovalCenter.Y + (float)(_ovalRadiusY * Math.Sin(_ovalAngle))
                );
            }
            else
            {
                // Waypoint movement for map 2 to 6
                var path = GameManager.Instance.Map.Path;
                if (path == null || path.Count == 0 || PathIndex >= path.Count)
                    return;

                float speed = Speed * (float)GameManager.Instance.GameSpeed;
                Vector2 target = path[PathIndex];
                Vector2 direction = target - Position;
                float distance = direction.Length();

                if (distance < speed)
                {
                    Position = target;
                    PathIndex++;
                    if (PathIndex >= path.Count)
                    {
                        // Enemy reached the end
                        GameManager.Instance.Player.GetType().GetProperty("Health").SetValue(GameManager.Instance.Player, GameManager.Instance.Player.Health - 5);
                        Health = 0;
                        GameManager.Instance.CurrentWave?.RemoveDefeatedEnemies();
                    }
                }
                else
                {
                    direction /= distance; // Normalize
                    Position += direction * speed;
                }
            }
        }




        public virtual void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Console.WriteLine("Enemy defeated.");
                GameManager.Instance.Player.GetType().GetProperty("Money").SetValue(GameManager.Instance.Player, GameManager.Instance.Player.Money + 25);
                GameForm.Instance?.AddFloatingText(Position, "+$25", SplashKitSDK.Color.Yellow);
                GameManager.Instance.CurrentWave?.RemoveDefeatedEnemies();
            }
        }

    }

    // Tower class
    internal class Tower
    {
        public Vector2 Position { get; set; }
        public float Range { get; protected set; }
        public int Damage { get; protected set; }
        public float Cooldown { get; protected set; }
        public int Cost { get; protected set; }
        public int UpgradeCost { get; protected set; }
        public SplashKitSDK.Bitmap Sprite { get; set; }
        public double CollisionRadius { get; set; } = 30; // Default, can be overridden
        private float _cooldownTimer = 0f;
        public bool CanFire => _cooldownTimer <= 0f;
        public int UpgradeLevel { get; protected set; } = 0;
        public int MaxUpgradeLevel { get; protected set; } = 3;
        public bool IsMaxed => UpgradeLevel >= MaxUpgradeLevel;

        public virtual void Upgrade()
        {
            if (IsMaxed)
            {
                Console.WriteLine("Tower is already maxxed!");
                return;
            }
            Damage += 5; // Increase damage
            Range += 1.0f; // Increase range
            Cooldown *= 0.9f; // Reduce cooldown (faster firing)
            UpgradeLevel++;
            Console.WriteLine("Tower upgraded: Damage, Range, Cooldown adjusted.");
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= deltaTime * (float)GameManager.Instance.GameSpeed;
        }


        public void TryFire(Enemy enemy)
        {
            if (CanFire)
            {
                Fire(enemy);
                _cooldownTimer = Cooldown;
            }
        }

        public virtual void Fire(Enemy enemy)
        {
            if (enemy == null) return;
            enemy.TakeDamage(Damage);
        }
    }

    // Map class
    internal class Map
    {
        private static Map _instance;
        public static Map Instance => _instance ??= new Map();

        public List<Vector2> Path { get; private set; }

        private Tile[,] grid;

        public Map()
        {
            // Example grid initialization (10x10 grid)
            grid = new Tile[10, 10];
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    grid[x, y] = new Tile { IsPath = false, IsTowerSpot = true };
                }
            }

            Path = new List<Vector2>();
            for (int x = 0; x <= 9; x++)
                Path.Add(new Vector2(x, 5));
        }
        private List<Tower> towers = new List<Tower>(); // Field to store towers

        public void LoadMap(int id)
        {
            Path = new List<Vector2>();
            if (id == 0)
            {
                // Oval path (original)
                double ovalRadiusX = 400;
                double ovalRadiusY = 240;
                Vector2 center = new Vector2(688, 596);
                int steps = 200;
                for (int i = 0; i <= steps; i++)
                {
                    double angle = (2 * Math.PI * i) / steps;
                    float x = (float)(center.X + ovalRadiusX * Math.Cos(angle));
                    float y = (float)(center.Y + ovalRadiusY * Math.Sin(angle));
                    Path.Add(new Vector2(x, y));
                }
            }
            else if (id == 1)
            {
                // Map 2: Zigzag
                Path.Add(new Vector2(100 - 304, 300 - 288));
                Path.Add(new Vector2(800 - 304, 300 - 288));
                Path.Add(new Vector2(800 - 304, 800 - 288));
                Path.Add(new Vector2(1300 - 304, 800 - 288));
            }
            else if (id == 2)
            {
                // Map 3: L-shape, then up, then right
                Path.Add(new Vector2(200 - 304, 900 - 288));
                Path.Add(new Vector2(200 - 304, 400 - 288));
                Path.Add(new Vector2(900 - 304, 400 - 288));
                Path.Add(new Vector2(900 - 304, 200 - 288));
                Path.Add(new Vector2(1300 - 304, 200 - 288));
            }
            else if (id == 3)
            {
                // Map 4: Down, right, down, right (staircase)
                Path.Add(new Vector2(200 - 304, 200 - 288));
                Path.Add(new Vector2(200 - 304, 600 - 288));
                Path.Add(new Vector2(600 - 304, 600 - 288));
                Path.Add(new Vector2(600 - 304, 1000 - 288));
                Path.Add(new Vector2(1200 - 304, 1000 - 288));
            }
            else if (id == 4)
            {
                // Map 5: Z-shape 
                Path.Add(new Vector2(400 - 304, 300 - 288));
                Path.Add(new Vector2(1200 - 304, 300 - 288));
                Path.Add(new Vector2(1200 - 304, 600 - 288));
                Path.Add(new Vector2(400 - 304, 600 - 288));
                Path.Add(new Vector2(400 - 304, 900 - 288));
                Path.Add(new Vector2(1200 - 304, 900 - 288));
            }
            else if (id == 5)
            {
                // Map 6: Rectangle perimeter (clockwise)
                Path.Add(new Vector2(300 - 304, 300 - 288));
                Path.Add(new Vector2(1200 - 304, 300 - 288));
                Path.Add(new Vector2(1200 - 304, 900 - 288));
                Path.Add(new Vector2(300 - 304, 900 - 288));
                Path.Add(new Vector2(300 - 304, 300 - 288));
            }
            Console.WriteLine($"Map {id} loaded. Path points: {Path.Count}");
        }





        public bool CanPlaceTower(Vector2 position)
        {
            // Prevent placing if any existing tower is within 40 units (center-to-center)
            foreach (var tower in GetTowers())
            {
                if (Vector2.Distance(tower.Position, position) < 40.0f)
                    return false;
            }
            return true;
        }

        public void AddTower(Tower tower, Vector2 position)
        {
            tower.Position = position; // Ensure the tower's position is set
            towers.Add(tower);
            Console.WriteLine($"Tower placed at {position}");
        }

        public List<Tower> GetTowers()
        {
            return towers; // Return the list of towers
        }
    }
    internal class Tile
    {
        public bool IsPath { get; set; }
        public bool IsTowerSpot { get; set; }
    }
}
