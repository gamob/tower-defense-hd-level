using SplashKitSDK;
using System;
using System.Numerics;

namespace ShapeDrawer.lib
{
    // Gunner Tower: Fast firing, moderate range and damage (buffed)
    internal class GunnerTower : Tower
    {
        public GunnerTower()
        {
            Range = 80.0f;         // Increased range
            Damage = 8;            // Increased damage
            Cooldown = 1.0f;       // Faster firing
            Cost = 120;            // Lowered cost
            UpgradeCost = 60;      // Lowered upgrade cost
            Sprite = SplashKit.LoadBitmap("Gunner", @"Resources/gunner.jpg");
            CollisionRadius = 80.0f;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            if (IsMaxed) return;
            Damage += 3;
            Range += 2.0f;
            Cooldown = Cooldown * 0.85f;
            Console.WriteLine("Gunner Tower upgraded!");
        }
    }

    // Shotgunner Tower: High damage, low range, slow firing (replaces GrenadeTower)
    // Shotgunner Tower: Higher damage, lower range, slow firing
    internal class ShotgunnerTower : Tower
    {
        public ShotgunnerTower()
        {
            Range = 45.0f;         // Lower range
            Damage = 30;           // Higher damage
            Cooldown = 1.7f;       // Slow firing
            Cost = 140;            // Balanced cost
            UpgradeCost = 80;      // Balanced upgrade cost
            Sprite = SplashKit.LoadBitmap("Shotgunner", @"Resources/shotgun.jpg");
            CollisionRadius = 45.0f;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            if (IsMaxed) return;
            Damage += 7;
            Range += 2.0f;
            Cooldown = Cooldown * 0.90f;
            Console.WriteLine("Shotgunner Tower upgraded!");
        }
    }


    // Sniper Tower: Long range, high damage, slow firing (buffed)
    internal class SniperTower : Tower
    {
        public SniperTower()
        {
            Range = 180.0f;        // Increased range
            Damage = 20;           // Increased damage
            Cooldown = 4.0f;       // Faster firing
            Cost = 160;            // Lowered cost
            UpgradeCost = 80;      // Lowered upgrade cost
            Sprite = SplashKit.LoadBitmap("Sniper", @"Resources/sniper.jpg");
            CollisionRadius = 180.0f;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            if (IsMaxed) return;
            Damage += 6;
            Range += 4.0f;
            Cooldown = Cooldown * 0.90f;
            Console.WriteLine("Sniper Tower upgraded!");
        }
    }

    // Farm Tower: Generates money each round, does not attack
    internal class FarmTower : Tower
    {
        public int MoneyPerRound { get; private set; } = 50;

        public FarmTower()
        {
            Range = 0.0f;
            Damage = 0;
            Cooldown = 0.0f;
            Cost = 150;
            UpgradeCost = 100;
            Sprite = SplashKit.LoadBitmap("Farm", @"Resources/farm.jpg");
            CollisionRadius = 0.0f;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            if (IsMaxed) return;
            MoneyPerRound += 30;
            Console.WriteLine("Farm Tower upgraded! Money per round: " + MoneyPerRound);
        }
    }



    // SplashTower: Medium damage, medium range, hits multiple enemies
    internal class SplashTower : Tower
    {
        public SplashTower()
        {
            Range = 80.0f;
            Damage = 30;
            Cooldown = 1.0f;
            Cost = 150;
            UpgradeCost = 90;
            Sprite = SplashKit.LoadBitmap("Splash", @"Resources/paintball.jpg");
            CollisionRadius = 80.0f;
            MaxUpgradeLevel = 5;
        }

        public override void Upgrade()
        {
            if (IsMaxed) return;
            Damage += 6;
            Range += 0.5f;
            UpgradeLevel++;
        }
    }

    // FreezeTower: Slows enemies on hit
    internal class FreezeTower : Tower
    {
        public FreezeTower()
        {
            Range = 70.0f;
            Damage = 5;
            Cooldown = 1.2f;
            Cost = 120;
            UpgradeCost = 80;
            Sprite = SplashKit.LoadBitmap("Freeze", @"Resources/freeze.jpg");
            CollisionRadius = 70.0f;
            MaxUpgradeLevel = 5;
        }
        public override void Fire(Enemy enemy)
        {
            base.Fire(enemy);
            enemy.Speed *= 0.5f;
        }
        public override void Upgrade()
        {
            if (IsMaxed) return;
            Damage += 3;
            Range += 0.5f;
            Cooldown *= 0.95f;
            UpgradeLevel++;
        }
    }

    // PoisonTower: Applies damage over time
    internal class PoisonTower : Tower
    {
        public PoisonTower()
        {
            Range = 60.0f;
            Damage = 4;
            Cooldown = 0.8f;
            Cost = 110;
            UpgradeCost = 70;
            Sprite = SplashKit.LoadBitmap("Poison", @"Resources/poison.jpg");
            CollisionRadius = 60.0f;
            MaxUpgradeLevel = 5;
        }
        public override void Fire(Enemy enemy)
        {
            base.Fire(enemy);
        }
        public override void Upgrade()
        {
            if (IsMaxed) return;
            Damage += 2;
            Range += 0.3f;
            UpgradeLevel++;
        }
    }

    // LaserTower: Constant beam, very fast cooldown, low damage
    internal class LaserTower : Tower
    {
        public LaserTower()
        {
            Range = 75.0f;
            Damage = 2;
            Cooldown = 0.05f;
            Cost = 160;
            UpgradeCost = 100;
            Sprite = SplashKit.LoadBitmap("Laser", @"Resources/laser.jpg");
            CollisionRadius = 75.0f;
            MaxUpgradeLevel = 5;
        }
        public override void Upgrade()
        {
            if (IsMaxed) return;
            Damage += 1;
            Range += 0.2f;
            Cooldown *= 0.9f;
            UpgradeLevel++;
        }
    }

}
