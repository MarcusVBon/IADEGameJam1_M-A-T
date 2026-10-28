using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace _3dgame;

public class GameTextures
{

    public Texture2D enemyTexture1 ;
    public Texture2D enemyShoot ;
    public Texture2D enemyBash ;

    public Texture2D enemyChaseIdle ;
    public Texture2D enemyChase ;
    public Texture2D enemyChaseAttack ;

    public Texture2D billinare;
    public Texture2D bloodSplat;

    public Texture2D crossHair ;
    public Texture2D pixel;
    public Texture2D gunUI;
    public Texture2D gunShootUI;
    public Texture2D kickUI;
    public Texture2D handEmpty;
    public Texture2D handRed;
    public Texture2D handBlue;
    public Texture2D handPurple;
}

public class Game1 : Game {
    private GraphicsDeviceManager graphics;
    private GameObject ground;

    //Song & Sounds

    SoundEffect gameSong;
    SoundEffect spinePull;
    SoundEffect spineCrush;
    SoundEffect playerHit;
    SoundEffect playerDeath;
    SoundEffect reloading;
    SoundEffect shooting;
    SoundEffect liquids;

    SoundEffectInstance effectInstance;

    // The grayscale shader effect.
    private Effect _grayscaleEffect;

    private Effect _pixelate;

    // The amount of saturation to provide the grayscale shader effect.
    private float _saturation = 0.0f;
    private float _contrast = 10.0f;
    private float _brightness = 1.0f;

    private float _pixelation = 8.0f;

    // The speed of the fade to grayscale effect.
    private const float FADE_SPEED = 0.02f;

    private RenderTarget2D sceneTarget;

    private RenderTarget2D pixelTarget;



    //MY STUFF
    private GameObject test;

    private float shootCooldown ;
    private int untilReload;
    private float kickCooldown ;
    private const float FireDelay = 0.5f;     // seconds between shots
    private const float KickDelay = 0.5f;     // seconds between shots
    private bool blockshoot = false ;
    private bool blockKick = false ;
    
    private readonly Matrix testWorld = Matrix.CreateScale(25f) * Matrix.CreateTranslation(60, 2.5f, 100);
    private GameObject corridor;
    // corridor runs along Z, entrance 5 units in front of the player's start (30, 2, 125)

    public GameTextures textures = new GameTextures() ;
    private List<Enemy> enemyList = new();

    private readonly Matrix levelMatrix = Matrix.CreateTranslation(30, 0, 100);
    private Camera gameCamera;
    private Player player;
    private readonly List<BoundingBox> colliders = new List<BoundingBox>();
    private KeyboardState currentKeyboardState = new KeyboardState();
    private MouseState currentMouseState = new MouseState();
    private MouseState previousMouseState = new MouseState();
    private bool mouseCentered = false;
    private int screenCenterX;
    private int screenCenterY;
    private float cameraYaw;
    private float cameraPitch;
    private double logTimer = 0d;
    private double totalMouseIdleTime = 0  ;
    private SpriteBatch spriteBatch;


    public Game1() {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        graphics.SynchronizeWithVerticalRetrace = true;

        graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        graphics.ToggleFullScreen();

        graphics.ApplyChanges();
        IsMouseVisible = false;
    }

    protected override void Initialize() {
        ground = new GameObject();

        spriteBatch = new SpriteBatch(GraphicsDevice);

        test = new GameObject();
        corridor = new GameObject();

        gameCamera = new Camera();
        player = new Player();
        screenCenterX = GraphicsDevice.Viewport.Width/2;
        screenCenterY = GraphicsDevice.Viewport.Height/2;
        Mouse.SetPosition(screenCenterX, screenCenterY);
        previousMouseState = Mouse.GetState();

        textures.enemyTexture1 = Content.Load<Texture2D>("Models/enemy");
        textures.enemyShoot = Content.Load<Texture2D>("Models/ShooterAttack");

        textures.enemyChaseAttack = Content.Load<Texture2D>("Models/ChaseAttack");
        textures.enemyChase = Content.Load<Texture2D>("Models/ChaseChase");
        textures.enemyChaseIdle = Content.Load<Texture2D>("Models/ChaseIdle");

        textures.billinare = Content.Load<Texture2D>("Models/boss");
        textures.bloodSplat = Content.Load<Texture2D>("Models/Splat");


        textures.gunUI = Content.Load<Texture2D>("Textures/GunIdle540") ;
        textures.gunShootUI = Content.Load<Texture2D>("Textures/GunFire") ;
        textures.kickUI = Content.Load<Texture2D>("Textures/boot") ;
        textures.handEmpty = Content.Load<Texture2D>("Textures/LeftHandBase") ;
        textures.handBlue = Content.Load<Texture2D>("Textures/LeftHandBlue") ;
        textures.handRed = Content.Load<Texture2D>("Textures/LeftHandRed") ;
        textures.handPurple = Content.Load<Texture2D>("Textures/LeftHandPurple") ;

        enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, 0f), 9f, 11f, true, true));

        enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(50f, 0f, 75f), 9f, 11f, true));

        enemyList.Add(new Enemy(GraphicsDevice, textures , new Vector3(75f, 0f, 75f), 9f, 11f, true));

        enemyList.Add(new Enemy(GraphicsDevice, textures , new Vector3(30f, 0f, 75f), 9f, 11f, true));

        enemyList.Add(new Enemy(GraphicsDevice, textures , new Vector3(120f, 0f, 120f), 9f, 11f, false));

        base.Initialize();
    }

    protected override void LoadContent() {
        ChangeRasterizerState(FillMode.Solid);
        ground.Model = Content.Load<Model>("Models/ground");

        gameSong = Content.Load<SoundEffect>("Audio/SparrowSong");
        playerDeath = Content.Load<SoundEffect>("Audio/PlayerDeath");
        playerHit = Content.Load<SoundEffect>("Audio/PlayerGetsHit");
        reloading = Content.Load<SoundEffect>("Audio/Reloading");
        shooting = Content.Load<SoundEffect>("Audio/Shooting");
        spineCrush = Content.Load<SoundEffect>("Audio/SpineCrush");
        spinePull = Content.Load<SoundEffect>("Audio/SpinePull");
        liquids = Content.Load<SoundEffect>("Audio/LiquidsMixing");

        effectInstance = gameSong.CreateInstance();

        effectInstance.Play();

        _grayscaleEffect = Content.Load<Effect>("Effects/FlashShader");
        _pixelate = Content.Load<Effect>("Effects/MainShader");
        

        test.Model = Content.Load<Model>("Models/test");

        textures.pixel = Content.Load<Texture2D>("Models/White");

        colliders.AddRange(ModelCollision.Build(test.Model, testWorld));
        corridor.Model = Content.Load<Model>("Models/corridor");
        colliders.AddRange(ModelCollision.Build(corridor.Model, levelMatrix, perMesh: true));
        player.Model = Content.Load<Model>("Models/player");
        player.CalculateBoundingSphere();

        sceneTarget = new RenderTarget2D(GraphicsDevice,
        GraphicsDevice.PresentationParameters.BackBufferWidth,
        GraphicsDevice.PresentationParameters.BackBufferHeight,
        false, SurfaceFormat.Color, DepthFormat.Depth24);
        pixelTarget = new RenderTarget2D(GraphicsDevice,
        GraphicsDevice.PresentationParameters.BackBufferWidth,
        GraphicsDevice.PresentationParameters.BackBufferHeight,
        false, SurfaceFormat.Color, DepthFormat.None);
    }

    protected override void Update(GameTime gameTime) {
        if (!IsActive) {
            base.Update(gameTime);
            return;
        }

        
        currentKeyboardState = Keyboard.GetState();

        // MOUSE & CAMERA INTERACTIONS 

        currentMouseState = Mouse.GetState();

        float maxPitchNegative = MathHelper.ToRadians(-85f);
        float maxPitchPositive = MathHelper.ToRadians(85f);

        if (currentMouseState != previousMouseState)
        {
            float deltaX = currentMouseState.X - previousMouseState.X;
            float deltaY = currentMouseState.Y - previousMouseState.Y;
            
            cameraYaw -= deltaX * GameConstants.sensitivity;
            float pitchDelta = -deltaY * GameConstants.sensitivity;
            float newPitch = cameraPitch + pitchDelta;

            if (!((cameraPitch >= maxPitchPositive && pitchDelta > 0f) || (cameraPitch <= maxPitchNegative && pitchDelta < 0f))) {
                cameraPitch = newPitch;
            }
            
            cameraPitch = MathHelper.Clamp(cameraPitch, maxPitchNegative, maxPitchPositive);   
        
            totalMouseIdleTime = 0 ; 
            previousMouseState = currentMouseState;

            
            // check if mouse is near/at border, if yes, move it to senter
            if (currentMouseState.X >= graphics.GraphicsDevice.Viewport.Width - (50* 1920/graphics.GraphicsDevice.Viewport.Width) || 
                currentMouseState.Y >= graphics.GraphicsDevice.Viewport.Height - (35 * 1080/graphics.GraphicsDevice.Viewport.Height) || 
                currentMouseState.X == 0 || currentMouseState.Y == 0)
            {
                Mouse.SetPosition(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);
                previousMouseState = Mouse.GetState();  
            }

        } else
        {
            previousMouseState = currentMouseState;
            
            totalMouseIdleTime += (float)gameTime.ElapsedGameTime.TotalSeconds ;
            if (totalMouseIdleTime >= 0.7 && Mouse.GetState().X != graphics.GraphicsDevice.Viewport.Width / 2 && Mouse.GetState().Y != graphics.GraphicsDevice.Viewport.Height / 2)
            {
                totalMouseIdleTime = 0 ; 
                Mouse.SetPosition(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);
                previousMouseState = Mouse.GetState();
            }
        }

        
        // Gun
        shootCooldown -= (float) gameTime.ElapsedGameTime.TotalSeconds ;
        if (shootCooldown < 0) 
            shootCooldown = 0 ;

        kickCooldown -= (float) gameTime.ElapsedGameTime.TotalSeconds ;
        if (kickCooldown < 0) 
            kickCooldown = 0 ;

        Shoot() ;

        player.Update((double) gameTime.ElapsedGameTime.TotalSeconds, currentKeyboardState, cameraYaw, colliders);

        
        foreach (Enemy enemy in enemyList)
        {
            if (enemy.enemyDead || enemy.billionare)
                continue ;

            //Console.WriteLine("Distance: " + Vector3.Distance(enemy.Position, player.Position)) ;
            if (enemy.chaser && !enemy.startAttack && Vector3.Distance(enemy.Position, player.Position) < 70 && Vector3.Distance(enemy.Position, player.Position) > 7)
            {
                Vector3 futurePosition = enemy.Position;

                Vector3 movementDir = player.Position - enemy.Position ;

                movementDir.Normalize() ;

                futurePosition.Y += movementDir.Y * 0.45f ;

                Vector3 futureX = enemy.Position + new Vector3(movementDir.X * 0.45f, 0, 0);
                if (enemy.ValidateMovement(futureX, colliders)) {
                    futurePosition.X = futureX.X;
                }

                Vector3 futureZ = futurePosition + new Vector3(0, 0, movementDir.Z * 0.45f);
                if (enemy.ValidateMovement(futureZ, colliders)) {
                    futurePosition.Z = futureZ.Z;
                }

                enemy.Position = futurePosition;
                enemy.texture = textures.enemyChase ;
            } else if (enemy.chaser && enemy.durationTimer == 0 )
            {
                enemy.texture = textures.enemyChaseIdle ;
            }

            enemy.Update((float)gameTime.ElapsedGameTime.TotalSeconds, player, colliders, textures) ;
        }

        gameCamera.Update(cameraYaw, cameraPitch, player.Position, graphics.GraphicsDevice.Viewport.AspectRatio);


        gameCamera.Update(cameraYaw, cameraPitch, player.Position, graphics.GraphicsDevice.Viewport.AspectRatio);

        if (currentKeyboardState.IsKeyDown(Keys.Escape)) {
            Exit();
        }

        logTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (logTimer >= 3) {
            Console.WriteLine("posX: " + player.Position.X + " posY: " + player.Position.Y + " posZ: " + player.Position.Z);
            logTimer = 0;
        }
        
        base.Update(gameTime);
    }

    private void Shoot()
    {
        if (Mouse.GetState().LeftButton == ButtonState.Released) 
        {
            blockshoot = false ;
        } 
        if (Mouse.GetState().RightButton == ButtonState.Released)
        {
            blockKick = false ;
        }

        Console.WriteLine("GUN COOLDOWN : " + shootCooldown + " KICK COOLDOWN: " + kickCooldown) ;
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && shootCooldown == 0 && !blockshoot)
        {
            
            blockshoot = true ;
            shooting.Play();
            shootCooldown = FireDelay;

            float cp = MathF.Cos(cameraPitch);
            var ray = new Ray(player.Position, new Vector3(-MathF.Sin(cameraYaw) * cp, MathF.Sin(cameraPitch), -MathF.Cos(cameraYaw) * cp));

            Enemy closest = null;
            float closestDistance = float.MaxValue;

            foreach (Enemy enemy in enemyList)
            {
                float? distance = enemy.rayIntersect(ray);
                if (distance.HasValue && distance.Value < closestDistance)
                {
                    closestDistance = distance.Value;
                    closest = enemy;
                }
            }

            float closestDistanceCollider = float.MaxValue;
            foreach (BoundingBox collider in colliders)
            {
                float? distance = ray.Intersects(collider) ;
                if (distance.HasValue && distance.Value < closestDistance)
                    closestDistanceCollider = distance.Value;
            }

            // NOTE: the room is one closed convex box, so a wall can never be between you and an enemy.
            // Once you add interior walls/obstacles, also ray-test them and ignore enemies that are farther away.
            
            if (closest == null|| closestDistanceCollider < closestDistance)
                return ;
            closest.enemyDead = true;
            closest.texture = textures.bloodSplat;

            player.switchHandState(1) ;
        } else if (Mouse.GetState().RightButton == ButtonState.Pressed && kickCooldown == 0 && !blockKick)
        {
            blockKick = true ;
            kickCooldown = FireDelay;
            float cp = MathF.Cos(cameraPitch);
            var ray = new Ray(player.Position, new Vector3(-MathF.Sin(cameraYaw) * cp, MathF.Sin(cameraPitch), -MathF.Cos(cameraYaw) * cp));

            Enemy closest = null;
            float closestDistance = float.MaxValue;

            foreach (Enemy enemy in enemyList)
            {
                if (Vector3.Distance(player.Position, enemy.Position) > 12)
                    continue ;
                

                float? distance = enemy.rayIntersect(ray);
                if (distance.HasValue && distance.Value < closestDistance)
                {
                    closestDistance = distance.Value;
                    closest = enemy;
                }
            }

            float closestDistanceCollider = float.MaxValue;
            foreach (BoundingBox collider in colliders)
            {
                float? distance = ray.Intersects(collider) ;
                if (distance.HasValue && distance.Value < closestDistance)
                    closestDistanceCollider = distance.Value;
            }
            
            if (closest == null|| closestDistanceCollider < closestDistance)
                //ADD WOOSH SOUND WHEN MISSING ENEMY
                return ;

            closest.enemyDead = true;
            playerHit.Play();
            closest.texture = textures.bloodSplat;
            player.switchHandState(2) ;
        }
    }

    private void DrawTerrain(Model model) {
        foreach (ModelMesh mesh in model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.World = Matrix.Identity;

                // use the matrices provided by the game camera
                effect.View = gameCamera.ViewMatrix;
                effect.Projection = gameCamera.ProjectionMatrix;
            }
            mesh.Draw();
        }
    }

    private void DrawTextures(Model model, Matrix world) {
        foreach (ModelMesh mesh in model.Meshes) {
            if (ModelCollision.IsCollider(mesh)) continue;   // col_ meshes are collision-only
            foreach (BasicEffect effect in mesh.Effects) {
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.TextureEnabled = true;
                effect.World = world;

                // use the matrices provided by the game camera
                effect.View = gameCamera.ViewMatrix;
                effect.Projection = gameCamera.ProjectionMatrix;
            }
            mesh.Draw();
        }
    }

    private Texture2D getCharacterHand()
    {
        if (player.handState == 0)
        {
            return textures.handEmpty ;
        } else if (player.handState == 4)
        {
            return textures.handPurple ;
        }
        return  textures.handEmpty ;
    }

    private Texture2D getCharacterHandItem()
    {
        if (player.handState == 1)
        {
            return textures.handRed ;
        } else if (player.handState == 2)
        {
            return textures.handBlue ;
        }
        return null ;
    }

    private void DrawHud(Player player)
    {
        int w = graphics.GraphicsDevice.Viewport.Width;
        int h = graphics.GraphicsDevice.Viewport.Height;

        // NonPremultiplied matches PNGs loaded with Texture2D.FromStream (soft edges look right).
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone);


        // Crosshair
        const int len = 8, gap = 4, thick = 2;
        int cx = w / 2, cy = h / 2;
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - gap - len, cy - thick / 2, len, thick), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx + gap, cy - thick / 2, len, thick), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - thick / 2, cy - gap - len, thick, len), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - thick / 2, cy + gap, thick, len), Color.White);

        // PURPLE BARRRRRRRRRRRRRRRRRRRR
        
        cx = w/ 40 ;
        cy =  h / 30;
        const int thickPurple = 50;
        const int LengthMax = 300;
        spriteBatch.Draw(textures.pixel, new Rectangle(cx, cy, (int) (LengthMax * (player.healthTimer / player.maxHealthTimer)) , thickPurple), Color.Purple);

        spriteBatch.Draw(textures.pixel, new Rectangle(cx, cy, (int) (LengthMax * (player.healthTimer / player.maxHealthTimer)) , thickPurple), Color.Purple);

        // gun

        Rectangle gunRect = new Rectangle(w - (w/1920 * 1250), h - (h/1080 * 1000), w/1920 * 1250, h/1080 * 1000) ;

        spriteBatch.Draw(shootCooldown < 0.3 ? textures.gunUI : textures.gunShootUI, gunRect, Color.White) ;

        // kick

        if (kickCooldown > 0.15) 
        {
            Rectangle kickRect = new Rectangle(w - (w/1920 * 1500), h - (h/1080 * 1000), w/1920 * 1250, h/1080 * 1000) ;

            spriteBatch.Draw(textures.kickUI, kickRect, Color.White) ;
        }

        // hand
        gunRect = new Rectangle(0, h - (h/1080 * 1000), w/1920 * 1250, h/1080 * 1000) ;

        spriteBatch.Draw(getCharacterHand(), gunRect, Color.White) ;
        if (getCharacterHandItem() != null)
            spriteBatch.Draw(getCharacterHandItem(), gunRect, Color.White) ;

        //0,28125

        spriteBatch.End();
    }
    
    protected override void Draw(GameTime gameTime) {

    //Draw the 3D scene into the render target
    GraphicsDevice.SetRenderTarget(sceneTarget);
    GraphicsDevice.Clear(Color.WhiteSmoke);

    GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    GraphicsDevice.BlendState = BlendState.Opaque;
    GraphicsDevice.RasterizerState = RasterizerState.CullNone;
    GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

    DrawTerrain(ground.Model);
    DrawTextures(test.Model, testWorld);
    DrawTextures(corridor.Model, levelMatrix);

    foreach (Enemy enemy in enemyList)
        enemy.Draw(GraphicsDevice, gameCamera.ViewMatrix, gameCamera.ProjectionMatrix, player.Position);

    //Draw that image to the screen through the shader
    GraphicsDevice.SetRenderTarget(pixelTarget);
    GraphicsDevice.Clear(Color.Black);

    _pixelate.Parameters["Pixelation"].SetValue(_pixelation);
    _pixelate.Parameters["ScreenSize"].SetValue(new Vector2(sceneTarget.Width, sceneTarget.Height));

    spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _pixelate);
    spriteBatch.Draw(sceneTarget, GraphicsDevice.Viewport.Bounds, Color.White);
    spriteBatch.End();


    GraphicsDevice.SetRenderTarget(null);
    GraphicsDevice.Clear(Color.Black);

    if(shootCooldown > 0.25f)
    {
    _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);
    _grayscaleEffect.Parameters["Contrast"].SetValue(_contrast);
    _grayscaleEffect.Parameters["Brightness"].SetValue(_brightness);
    spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _grayscaleEffect);
    }

    else
    {
         spriteBatch.Begin(samplerState: SamplerState.PointClamp);   
    }
    spriteBatch.Draw(pixelTarget, GraphicsDevice.Viewport.Bounds, Color.White);
    spriteBatch.End();
    

    //HUD with no shader
    DrawHud(player);

    base.Draw(gameTime);
}

    private RasterizerState ChangeRasterizerState(FillMode fillmode, CullMode cullMode = CullMode.None) {
        RasterizerState rasterizerState = new RasterizerState() { 
            FillMode = fillmode,
            CullMode = cullMode 
        };
        graphics.GraphicsDevice.RasterizerState = rasterizerState;
        return rasterizerState;
    }
}