using System;
using System.Collections.Generic;
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
    public Texture2D crossHair ;
    public Texture2D pixel;
    
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

    private float shootDelay ;
    private const float FireDelay = 0.25f;     // seconds between shots
    
    private readonly Matrix testWorld = Matrix.CreateScale(25f) * Matrix.CreateTranslation(60, 2.5f, 100);
    private GameObject corridor;
    // corridor runs along Z, entrance 5 units in front of the player's start (30, 2, 125)

    private GameTextures textures = new GameTextures() ;
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

        enemyList.Add(new Enemy(GraphicsDevice, textures.enemyTexture1 , new Vector3(50f, 0f, 75f), 9f, 11f, true));
        enemyList.Add(new Enemy(GraphicsDevice, textures.enemyTexture1 , new Vector3(120f, 0f, 120f), 9f, 11f, false));

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
        shootDelay = MathF.Max(0f, shootDelay - gameTime.ElapsedGameTime.Seconds);

        Shoot() ;

        player.Update((double) gameTime.ElapsedGameTime.TotalSeconds, currentKeyboardState, cameraYaw, colliders);

        
        foreach (Enemy enemy in enemyList)
        {
            if (enemy.enemyDead)
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
                

            }
            enemy.Update((float)gameTime.ElapsedGameTime.TotalSeconds, player, colliders) ;
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
        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
        {
            shootDelay = FireDelay;

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

    private void DrawHud()
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
    _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);
    _grayscaleEffect.Parameters["Contrast"].SetValue(_contrast);
    _grayscaleEffect.Parameters["Brightness"].SetValue(_brightness);

    spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _grayscaleEffect);
    spriteBatch.Draw(pixelTarget, GraphicsDevice.Viewport.Bounds, Color.White);
    spriteBatch.End();

    //HUD with no shader
    DrawHud();

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