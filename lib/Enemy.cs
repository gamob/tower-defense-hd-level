using System;
using SplashKitSDK;

namespace ShapeDrawer.lib
{
    // Normal Enemy: Standard walking enemy
    internal class NormalEnemy : Enemy
    {
        private static readonly Bitmap NormalSprite = SplashKit.LoadBitmap("Normal", @"Resources/normal.jpg");

        public NormalEnemy()
        {
            Health = 50; // Moderate health
            Speed = 0.1f; // Moderate speed
            Sprite = NormalSprite;
        }

    }

    // Air Enemy: Flies above some tower types
    internal class AirEnemy : Enemy
    {
        private static readonly Bitmap AirSprite = SplashKit.LoadBitmap("Fly", @"Resources/fly.jpg");

        public AirEnemy()
        {
            Health = 30;
            Speed = 0.2f;
            Sprite = AirSprite;
        }
        // ...
    }

    // Boss Enemy: High HP, slow but deadly
    internal class BossEnemy : Enemy
    {
        private static readonly Bitmap BossSprite = SplashKit.LoadBitmap("Boss", @"Resources/boss.jpg");

        public BossEnemy()
        {
            Health = 3000;
            Speed = 0.02f;
            Sprite = BossSprite;
        }
        // ...
    }
}
