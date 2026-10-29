using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct2D1.Effects;
using SharpDX.XAudio2;
using Microsoft.Xna.Framework.Media;
using System.Drawing.Text;
using SharpDX.MediaFoundation;

namespace _3dgame;

public class GameTextures
{

    public Texture2D enemyTexture1 ;
    public Texture2D enemyShoot ;
    public Texture2D enemyBash ;

    public Texture2D button;
    public Texture2D logo;

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
    public Texture2D GameOver;
    public Texture2D bossKill;

    // TUTORIAL & EXPO
    public Texture2D tutorial;
    public Texture2D expo;

}

public class Game1 : Game {

    enum GameState {MainMenu, Options, Playing, Paused, GameOver, BossKilled, tutorialExpo}
    private GameState state = GameState.MainMenu;
    private KeyboardState previousKeyboardState;

    private readonly string[] menuLabels = { "Start Game", "Options", "Exit Game"};
    private MouseState menuPrevMouse;

    private GraphicsDeviceManager graphics;
    private GameObject ground;

    private bool PressedOnce(Keys key) =>
    currentKeyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyUp(key);

    //Transition Effect
    private enum TransitionPhase { None, Closing, Closed, Opening }
    private TransitionPhase transition = TransitionPhase.None;
    private float transitionTime;
    private const float TransitionDuration = 0.5f;   // seconds for each half
    private Action onScreenClosed;

    private enum ElevatorStep { None, Closing, Ding, Opening }
    private ElevatorStep elevatorStep = ElevatorStep.None;
    private Action elevatorAction;

    //Song & Sounds
    SoundEffect introSong;
    SoundEffectInstance introSongInstance;
    SoundEffect noIntroSong;
    SoundEffectInstance noIntroSongInstance;
    SoundEffect mainMenuSong;
    SoundEffectInstance mainMenuSongInstance;
    SoundEffect elevatorDing;
    SoundEffectInstance elevatorDingInstance;
    SoundEffect elevatorDoor;
    SoundEffectInstance elevatorDoorInstance;
    SoundEffect spineCrush;
    SoundEffectInstance spineCrushInstance;
    SoundEffect playerHit;
    SoundEffectInstance playerHitInstance;
    SoundEffect playerDeath;
    SoundEffectInstance playerDeathInstance;
    SoundEffect reloading;
    SoundEffectInstance reloadingInstance;
    SoundEffect shooting;
    SoundEffectInstance shootingInstance;
    SoundEffect liquids;
    SoundEffectInstance liquidsInstance;

    // The grayscale shader effect.
    private Effect _grayscaleEffect;

    private Effect _pixelate;

    // The amount of saturation to provide the grayscale shader effect.
    private float _saturation = 0.0f;
    private float _contrast = 10.0f;
    private float _brightness = 3.0f;

    private float _pixelation = 8.0f;

    // The speed of the fade to grayscale effect.
    private const float FADE_SPEED = 0.02f;

    private RenderTarget2D sceneTarget;

    private RenderTarget2D pixelTarget;

    public int score = 0;

    private Rectangle GetButtonRect (int i)
    {
        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;

        int bw = (int)(400*w / 1920f);
        int bh = (int)(100 * h / 1080f);
        int gap = (int)(30 * h / 1080f);

        int totalHeight = 3*bh + 2 * gap;
        int x = (w - bw) /2;
        int y = (h - totalHeight) / 2 + i * (bh + gap);
        return new Rectangle(x,y,bw,bh);
    }

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
    // the current room, picked in order by currentLevel -> LoadLevel()
    private GameObject level;
    private int currentLevel = 1;
    // content manager for the cur level
    private ContentManager levelContent;
    private readonly Random random = new Random();

    public GameTextures textures = new GameTextures() ;
    private List<Enemy> enemyList = new();

    // WHERE THE LEVEL SPAWNS
    private readonly Matrix levelMatrix = Matrix.CreateTranslation(0, 0, 0);
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
    private int tutorialPage = 0  ;
    private SpriteBatch spriteBatch;
    private SpriteFont debugFont; // DELETE LATER


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

    public void restartGame()
    {
        currentLevel = 1;
        LoadLevel();
        player.ResetToStart();    // check that this also resets health and playerDead
        cameraYaw = 0f;
        cameraPitch = 0f;
    }

    protected override void Initialize() {
        ground = new GameObject();

        spriteBatch = new SpriteBatch(GraphicsDevice);
    
        test = new GameObject();
        level = new GameObject();
        levelContent = new ContentManager(Services, Content.RootDirectory);

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

        
        textures.GameOver = Content.Load<Texture2D>("Models/Gameover");
        textures.bossKill = Content.Load<Texture2D>("Models/win");
        textures.tutorial = Content.Load<Texture2D>("Models/Gameover");
        textures.expo = Content.Load<Texture2D>("Models/win");


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

        introSong = Content.Load<SoundEffect>("Audio/SparrowIntro");
        introSongInstance = introSong.CreateInstance();
        noIntroSong = Content.Load<SoundEffect>("Audio/SparrowNoIntro");
        noIntroSongInstance = noIntroSong.CreateInstance();
        mainMenuSong = Content.Load<SoundEffect>("Audio/SparrowMenu");
        mainMenuSongInstance = mainMenuSong.CreateInstance();
        elevatorDing = Content.Load<SoundEffect>("Audio/ElevatorDing");
        elevatorDingInstance = elevatorDing.CreateInstance();
        elevatorDoor = Content.Load<SoundEffect>("Audio/ElevatorDoor");
        elevatorDoorInstance = elevatorDoor.CreateInstance();
        playerDeath = Content.Load<SoundEffect>("Audio/PlayerDeath");
        playerDeathInstance = playerDeath.CreateInstance();
        playerHit = Content.Load<SoundEffect>("Audio/PlayerGetsHit");
        playerHitInstance = playerHit.CreateInstance();
        reloading = Content.Load<SoundEffect>("Audio/Reloading");
        reloadingInstance = reloading.CreateInstance();
        shooting = Content.Load<SoundEffect>("Audio/Shooting");
        shootingInstance = shooting.CreateInstance();
        spineCrush = Content.Load<SoundEffect>("Audio/SpineCrush");
        spineCrushInstance = spineCrush.CreateInstance();
        liquids = Content.Load<SoundEffect>("Audio/LiquidsMixing");
        liquidsInstance = liquids.CreateInstance();

        Enemy.LoadSounds(Content);

        player.SpineCrushSound = spineCrushInstance;
        player.LiquidsSound = liquidsInstance;
        player.PlayerDeathSound = playerDeathInstance;

        

        _grayscaleEffect = Content.Load<Effect>("Effects/FlashShader");
        _pixelate = Content.Load<Effect>("Effects/MainShader");

        test.Model = Content.Load<Model>("Models/test");

        textures.pixel = Content.Load<Texture2D>("Models/White");
        debugFont = Content.Load<SpriteFont>("Fonts/Debug"); // DELETE LATER
        textures.button = Content.Load<Texture2D>("Textures/Button");

        player.Model = Content.Load<Model>("Models/player");
        player.CalculateBoundingSphere();

        LoadLevel();

        sceneTarget = new RenderTarget2D(GraphicsDevice,
        GraphicsDevice.PresentationParameters.BackBufferWidth,
        GraphicsDevice.PresentationParameters.BackBufferHeight,
        false, SurfaceFormat.Color, DepthFormat.Depth24);
        pixelTarget = new RenderTarget2D(GraphicsDevice,
        GraphicsDevice.PresentationParameters.BackBufferWidth,
        GraphicsDevice.PresentationParameters.BackBufferHeight,
        false, SurfaceFormat.Color, DepthFormat.None);
    }

//good practice bellow, not sure what it does LMAOOOO
    protected override void UnloadContent() {
        levelContent.Unload();
        base.UnloadContent();
    }

    private void LoadLevel() {        //LEVEL INITIALISED HERE
        levelContent.Unload();
        enemyList.Clear();
        player.healthTimer = player.maxHealthTimer;


        switch(currentLevel)
        {
        case 1:
            level.Model = levelContent.Load<Model>("Models/level1");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, -92f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(88f, 0f, -92f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(88f, 0f, -180f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-68f, 0f, -180f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-68f, 0f, -252f), 9f, 11f, false, false));

            break;
        case 2:
            level.Model = levelContent.Load<Model>("Models/level2");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(40f, 0f, -90f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-40f, 0f, -90f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-40f, 0f, -172f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(40f, 0f, -172f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-32f, 0f, -230f), 9f, 11f, true, false));

            break;
        case 3:
            level.Model = levelContent.Load<Model>("Models/level3");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-38f, 0f, -176f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(36f, 0f, -176f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(94f, 0f, -152f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(94f, 0f, -82f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(194f, 0f, -50f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(100f, 0f, -30f), 9f, 11f, true, false));

            break;
        case 4:
            level.Model = levelContent.Load<Model>("Models/level4");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, -204f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-88f, 0f, -204f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-88f, 0f, -116f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(68f, 0f, -116f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(68f, 0f, -44f), 9f, 11f, true, false));

            break;
        case 5:
            level.Model = levelContent.Load<Model>("Models/Level5");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(76f, 0f, -72f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-78f, 0f, -72f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-80f, 0f, -120f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(80f, 0f, -120f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, -170f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-70f, 0f, -262f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-150f, 0f, -210f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-20f, 0f, -210f), 9f, 11f, true, false));

            break;
        case 6:
            level.Model = levelContent.Load<Model>("Models/level6");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(178f, 0f, -110f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(100f, 0f, -200f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(80f, 0f, -120f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(186f, 0f, -234f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(60f, 0f, -236f), 9f, 11f, false, false));

            break;
        case 7:
            level.Model = levelContent.Load<Model>("Models/level7");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(20f, 0f, -120f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(-80f, 0f, -160f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, -160f), 9f, 11f, true, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(80f, 0f, -160f), 9f, 11f, false, false));
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, -200f), 9f, 11f, true, false));

            break;
        case 8:
            level.Model = levelContent.Load<Model>("Models/bossLevel");
            enemyList.Add(new Enemy(GraphicsDevice, textures, new Vector3(0f, 0f, -206f), 9f, 11f, true, true));

            break;
        default:
            currentLevel = 1;
            LoadLevel();
            return;
        }

        colliders.Clear();
        colliders.AddRange(ModelCollision.Build(test.Model, testWorld));
        colliders.AddRange(ModelCollision.Build(level.Model, levelMatrix, perMesh: true));


    }

    private void CompleteLevel() {
        Console.WriteLine("YOU DID IT");
        currentLevel++;
        LoadLevel();
        player.ResetToStart();
        cameraYaw = 0f;
        cameraPitch = 0f;
    }

    public void renderGameOverScreen()
    {
        Console.WriteLine("RENDER!") ;
        int w = graphics.GraphicsDevice.Viewport.Width;
        int h = graphics.GraphicsDevice.Viewport.Height;

        // NonPremultiplied matches PNGs loaded with Texture2D.FromStream (soft edges look right).
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone);
        spriteBatch.Draw(textures.GameOver, new Rectangle(0, 0, w, h), Color.Purple);

        spriteBatch.End();
    }

    public void renderKilledBossScreen(){
        int w = graphics.GraphicsDevice.Viewport.Width;
        int h = graphics.GraphicsDevice.Viewport.Height;

        // NonPremultiplied matches PNGs loaded with Texture2D.FromStream (soft edges look right).
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone);

        spriteBatch.Draw(textures.bossKill, new Rectangle(0, 0, w, h), Color.White);

        spriteBatch.End();
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateTransition(gameTime);
        UpdateElevator();

        if (!IsActive) { base.Update(gameTime); return; }
        currentKeyboardState = Keyboard.GetState();

        switch (state)
        {
            case GameState.MainMenu: UpdateMainMenu(); break;
            case GameState.Playing: UpdatePlaying(gameTime); break;
            case GameState.Paused: UpdatePause(); break;
            case GameState.GameOver: UpdateGameOver(); break;
            case GameState.BossKilled: UpdateKilledBoss(); break;
            case GameState.tutorialExpo: UpdateTutorialExpo(); break;
        }

        previousKeyboardState = currentKeyboardState;
        base.Update(gameTime);
    }

    private void UpdateMainMenu()
    {
        mainMenuSongInstance.Play();
        mainMenuSongInstance.IsLooped = true;

        IsMouseVisible = true;

        MouseState mouse = Mouse.GetState();
        bool clicked = mouse.LeftButton == ButtonState.Pressed && menuPrevMouse.LeftButton == ButtonState.Released;

        if (clicked)
        {
            for (int i = 0; i < menuLabels.Length; i++)
            {
                if (!GetButtonRect(i).Contains(mouse.Position)) continue;

                switch (i)
                {
                    case 0: state = GameState.tutorialExpo; tutorialPage = 0 ; break; //startGame();break;
                    case 2: Exit(); break;
                }
            }
        }
        menuPrevMouse = mouse;
    }

    private void StartGame()
    {
        restartGame();
        gameCamera.Update(cameraYaw, cameraPitch, player.Position, GraphicsDevice.Viewport.AspectRatio);

        
        player.ResetToStart();
        LoadLevel() ;

        state = GameState.Playing;
        IsMouseVisible = false;

        Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height /2);
        previousMouseState = Mouse. GetState();
        blockshoot = true;
    }

    private void UpdateTutorialExpo()
    {
        if (currentKeyboardState.IsKeyUp(Keys.Space)) 
        {
            blockshoot = false ;
        } 

        Console.WriteLine("UPDATE!") ;
        currentKeyboardState = Keyboard.GetState();
        if (currentKeyboardState.IsKeyDown(Keys.Space) && tutorialPage == 0 && !blockshoot)
        {
            tutorialPage = 1 ;
            blockshoot = true ;
        } else if (currentKeyboardState.IsKeyDown(Keys.Space) && tutorialPage == 1 && !blockshoot)
        {
            StartTransition(() => StartGame());
        }
    }

    public void renderTutorialExpo()
    {
        Console.WriteLine("RENDER!") ;
        int w = graphics.GraphicsDevice.Viewport.Width;
        int h = graphics.GraphicsDevice.Viewport.Height;

        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone);

        if (tutorialPage == 0 )
        {
            spriteBatch.Draw(textures.expo, new Rectangle(0, 0, w, h), Color.Purple);
        } else
        {
            spriteBatch.Draw(textures.tutorial, new Rectangle(0, 0, w, h), Color.Purple);
        }

        // NonPremultiplied matches PNGs loaded with Texture2D.FromStream (soft edges look right).

        spriteBatch.End();
    }

    private void UpdateOptions()
    {
        if (PressedOnce(Keys.Escape))
            state = GameState.MainMenu;
    }

    private void UpdatePause()
    {
        
    }

    private void UpdateGameOver()
    {
        Console.WriteLine("UPDATE!") ;
        currentKeyboardState = Keyboard.GetState();
        if (currentKeyboardState.IsKeyDown(Keys.Space))
        {
            Console.WriteLine("RESTART!") ;
            currentLevel -= 1 ;
            if (currentLevel < 0)
            {
                currentLevel = 0 ;
            }
            LoadLevel() ;
            player.ResetToStart();
        }
    }

    private void UpdateKilledBoss()
    {
        Console.WriteLine("UPDATE!") ;
        currentKeyboardState = Keyboard.GetState();
        if (currentKeyboardState.IsKeyDown(Keys.Space))
        {
            state = GameState.MainMenu ;
        }    
    }

    protected void UpdatePlaying(GameTime gameTime) {
        mainMenuSongInstance.Stop();
        noIntroSongInstance.Play();

        if (elevatorStep != ElevatorStep.None)
        {
            previousMouseState = Mouse.GetState();   // stops the camera from jumping when play resumes
            return;
        }
        
        if (player.playerDead)
        {
            currentKeyboardState = Keyboard.GetState();
            if (PressedOnce(Keys.Escape))
            StartTransition(() => GoToMainMenu());
            if (currentKeyboardState.IsKeyDown(Keys.Space))
            {
                //renderGameOverScreen() ;
                Console.WriteLine("RESTART!") ;
                currentLevel -= 1 ;
                if (currentLevel < 0)
                {
                    currentLevel = 0 ;
                }
                player.ResetToStart();
                LoadLevel() ;
            }
            return ;
        }

        if (!IsActive) {
            base.Update(gameTime);
            return;
        }

        //MUSIC LOOPING

        mainMenuSongInstance.Stop();
        if (elevatorDoorInstance.State != SoundState.Playing && noIntroSongInstance.State != SoundState.Playing)
            noIntroSongInstance.Play();
        Console.WriteLine("playerPosX:" + player.Position.X);
        

        
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
            if (enemy.enemyDead)
            {
                enemy.deathTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds ;
                continue ;
            }
            if (enemy.billionare)
                continue ;

            //Console.WriteLine("Distance: " + Vector3.Distance(enemy.Position, player.Position)) ;
            if (enemy.chaser && !enemy.startAttack && Vector3.Distance(enemy.Position, player.Position) < 250 && Vector3.Distance(enemy.Position, player.Position) > 7 && enemy.canSeePlayer(player, colliders))
            {
                Vector3 futurePosition = enemy.Position;

                Vector3 movementDir = player.Position - enemy.Position ;

                movementDir.Normalize() ;
                                                                        //SPEED->
                Vector3 futureX = enemy.Position + new Vector3(movementDir.X * 1f, 0, 0);
                if (enemy.ValidateMovement(futureX, colliders)) {
                    futurePosition.X = futureX.X;
                }

                Vector3 futureZ = futurePosition + new Vector3(0, 0, movementDir.Z * 1f);
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

        enemyList.RemoveAll(enemy => enemy.enemyDead && enemy.deathTimer <= 0) ;

        if (player.Position.Z <= GameConstants.LevelEndZ)
            StartTransition(() => CompleteLevel());

        gameCamera.Update(cameraYaw, cameraPitch, player.Position, graphics.GraphicsDevice.Viewport.AspectRatio);

        if (currentKeyboardState.IsKeyDown(Keys.Escape))
        StartTransition(() => GoToMainMenu());

        logTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (logTimer >= 3) {
            Console.WriteLine("posX: " + player.Position.X + " posY: " + player.Position.Y + " posZ: " + player.Position.Z);
            logTimer = 0;
        }
        
        base.Update(gameTime);
    }

private void CloseScreen(Action whenClosed = null)
{
    if (transition == TransitionPhase.Closing || transition == TransitionPhase.Closed) return;
    onScreenClosed = whenClosed;
    transition = TransitionPhase.Closing;
    transitionTime = 0f;
}

// Open the bars again (only works if the screen is fully closed).
private void OpenScreen()
{
    if (transition != TransitionPhase.Closed) return;
    transition = TransitionPhase.Opening;
    transitionTime = 0f;
}

// Old behavior in one call: close, run the action, then open.
private void StartTransition(Action whenClosed)
{
    if (elevatorStep != ElevatorStep.None) return;   // already running

    elevatorAction = whenClosed;
    elevatorStep = ElevatorStep.Closing;

    elevatorDoorInstance.Stop();
    elevatorDoorInstance.Play();     // door sound starts as the screen closes
    CloseScreen();
}

private void GoToMainMenu()
{
    state = GameState.MainMenu;
    IsMouseVisible = true;
}

private void UpdateElevator()
{
    switch (elevatorStep)
    {
        case ElevatorStep.Closing:
            // wait until the door sound is finished AND the screen is fully closed
            if (elevatorDoorInstance.State != SoundState.Playing && transition == TransitionPhase.Closed)
            {
                elevatorAction?.Invoke();          // swap level / start game while it's black
                elevatorAction = null;

                elevatorDingInstance.Play();
                elevatorStep = ElevatorStep.Ding;

                mainMenuSongInstance.Stop();
                noIntroSongInstance.Stop();
            }
            break;

        case ElevatorStep.Ding:
            // wait until the ding is finished
            if (elevatorDingInstance.State != SoundState.Playing)
            {
                elevatorDoorInstance.Play();       // door sound again
                OpenScreen();                      // bars open at the same time
                elevatorStep = ElevatorStep.Opening;
            }
            break;

        case ElevatorStep.Opening:
            if (transition == TransitionPhase.None)
                elevatorStep = ElevatorStep.None;  // done
            break;
    }
}

private void UpdateTransition(GameTime gameTime)
{
    if (transition == TransitionPhase.None || transition == TransitionPhase.Closed) return;

    transitionTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (transitionTime < TransitionDuration) return;

    transitionTime = 0f;
    if (transition == TransitionPhase.Closing)
    {
        transition = TransitionPhase.Closed;      // set first, so the callback can call OpenScreen()
        Action callback = onScreenClosed;
        onScreenClosed = null;
        callback?.Invoke();
    }
    else
    {
        transition = TransitionPhase.None;
    }
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
                if (enemy.enemyDead)
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

            // NOTE: the room is one closed convex box, so a wall can never be between you and an enemy.
            // Once you add interior walls/obstacles, also ray-test them and ignore enemies that are farther away.
            
            if (closest == null|| closestDistanceCollider < closestDistance)
                return ;
            closest.enemyDead = true;
            closest.texture = textures.bloodSplat;

            if (closest.billionare)
            {
                state = GameState.BossKilled ;
                Console.WriteLine("STATE" + state) ;
            }

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
                if (enemy.enemyDead || Vector3.Distance(player.Position, enemy.Position) > 12)
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
            if (closest.billionare)
            {
                state = GameState.BossKilled ;
                Console.WriteLine("STATE" + state) ;
            }
            playerHit.Play();
            closest.texture = textures.bloodSplat;
            player.switchHandState(2) ;
        }
    }

    private void DrawTerrain(Model model) {
        foreach (ModelMesh mesh in model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                //effect.EnableDefaultLighting();

                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.SpecularColor = Vector3.Zero;

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
                //effect.EnableDefaultLighting();

                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.SpecularColor = Vector3.Zero;

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

private void DrawTransition()
{
    if (transition == TransitionPhase.None) return;

    float t = MathHelper.Clamp(transitionTime / TransitionDuration, 0f, 1f);
    float closed = transition switch
    {
        TransitionPhase.Closing => t,
        TransitionPhase.Closed  => 1f,
        TransitionPhase.Opening => 1f - t,
        _ => 0f
    };
    closed = MathHelper.SmoothStep(0f, 1f, closed);

    int w = GraphicsDevice.Viewport.Width;
    int h = GraphicsDevice.Viewport.Height;
    int barWidth = (int)MathF.Ceiling(w / 2f * closed);

    spriteBatch.Begin();
    spriteBatch.Draw(textures.pixel, new Rectangle(0, 0, barWidth, h), Color.Black);
    spriteBatch.Draw(textures.pixel, new Rectangle(w - barWidth, 0, barWidth, h), Color.Black);
    spriteBatch.End();
}
    
    private void DrawHud(Player player)
    {
        if (player.playerDead)
            return ;

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

        //string zText = "Z: " + player.Position.Z.ToString("0.0") + " / " + GameConstants.LevelEndZ; // DELETE LATER
        //spriteBatch.DrawString(debugFont, zText, new Vector2(21, 21), Color.Black); // DELETE LATER (shadow)
        //spriteBatch.DrawString(debugFont, zText, new Vector2(20, 20), Color.Yellow); // DELETE LATER
        // PURPLE BARRRRRRRRRRRRRRRRRRRR
        
        cx = w/ 40 ;
        cy =  h / 30;
        const int thickPurple = 50;
        const int LengthMax = 300;
        spriteBatch.Draw(textures.pixel, new Rectangle(cx, cy, (int) (LengthMax * (player.healthTimer / player.maxHealthTimer)) , thickPurple), Color.Purple);

        spriteBatch.Draw(textures.pixel, new Rectangle(cx, cy, (int) (LengthMax * (player.healthTimer / player.maxHealthTimer)) , thickPurple), Color.Purple);

        // gun

        Rectangle gunRect = new Rectangle(w - (w/1920 * 1700), h - (h/1080 * 1350), w/1920 * 1700, h/1080 * 1350) ;

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

    private void DrawMainMenu()
    {
        GraphicsDevice.Clear(Color.Black);
        Point mouse = Mouse.GetState().Position;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        for (int i = 0; i < menuLabels.Length; i++)
        {
            Rectangle rect = GetButtonRect(i);
            bool hover = rect.Contains(mouse);

            spriteBatch.Draw(textures.button, rect, hover ? Color.White : Color.Gray);

            Vector2 size = debugFont.MeasureString(menuLabels[i]);
            Vector2 pos = new Vector2(rect.X + (rect.Width - size.X) / 2,
                                        rect.Y + (rect.Height - size.Y) / 2);
            spriteBatch.DrawString(debugFont,menuLabels[i], pos, hover ? Color.Yellow : Color.White);
        }

        spriteBatch.End();
    }

    private void DrawOptions()
    {
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin();
        spriteBatch.DrawString(debugFont, "Options (Esc to go back)", new Vector2(100,100) , Color.White);
        spriteBatch.End();
    }

    private void DrawPauseMenu()
    {
        
    }

    protected override void Draw(GameTime gameTime)
    {
        switch (state)
        {
            case GameState.MainMenu:
            GraphicsDevice.Clear(Color.Black);
            DrawMainMenu();
            break;
            case GameState.Playing:
            DrawScene(gameTime); DrawHud(player);
            break;
            case GameState.Paused:
            DrawScene(gameTime); DrawPauseMenu();
            break;
            case GameState.GameOver:
            GraphicsDevice.Clear(Color.Black);
            renderGameOverScreen();
            break;
            case GameState.BossKilled:
            GraphicsDevice.Clear(Color.Black);
            renderKilledBossScreen();
            break;
            case GameState.tutorialExpo:
            GraphicsDevice.Clear(Color.Black);
            renderTutorialExpo();
            break;
        }
        DrawTransition();
        base.Draw(gameTime);
    }
    protected void DrawScene(GameTime gameTime) {
        if (player.playerDead)
        {
            GraphicsDevice.Clear(Color.Black);
            renderGameOverScreen() ;
            return ;
        }

    //Draw the 3D scene into the render target
    GraphicsDevice.SetRenderTarget(sceneTarget);
    GraphicsDevice.Clear(Color.WhiteSmoke);

    GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    GraphicsDevice.BlendState = BlendState.Opaque;
    GraphicsDevice.RasterizerState = RasterizerState.CullNone;
    GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

    DrawTerrain(ground.Model);   // GROUND IS RENDERED HERE
    DrawTextures(test.Model, testWorld);
    DrawTextures(level.Model, levelMatrix);

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
    _saturation = 0.0f;
    _contrast = 10.0f;
    _brightness = 3.0f;
    _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);
    _grayscaleEffect.Parameters["Contrast"].SetValue(_contrast);
    _grayscaleEffect.Parameters["Brightness"].SetValue(_brightness);
    spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _grayscaleEffect);
    }

    else
    {
    _saturation = 1.2f;
    _contrast = 1.0f;
    _brightness = 0.0f;
    _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);
    _grayscaleEffect.Parameters["Contrast"].SetValue(_contrast);
    _grayscaleEffect.Parameters["Brightness"].SetValue(_brightness);
    spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _grayscaleEffect);  
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