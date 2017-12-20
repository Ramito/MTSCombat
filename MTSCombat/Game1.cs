using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MTSCombat.Render;
using MTSCombat.Simulation;

namespace MTSCombat
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {

        private const int kArenaWidth = 1280;
        private const int kArenaHeight = 720;
        private GraphicsDeviceManager mGraphics;
        private MTSCombatRenderer mMTSCRenderer;
        private MTSCombatGame mMTSGame;
        
        public Game1()
        {
            mGraphics = new GraphicsDeviceManager(this);
            MatchCurrentResolution(mGraphics);
            Content.RootDirectory = "Content";
        }

        private static void MatchCurrentResolution(GraphicsDeviceManager graphics)
        {
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            //TODO: Toggling full screen makes debugging a pain
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None;
            mGraphics.GraphicsDevice.RasterizerState = rs;

            PrimitiveRenderer primitiveRenderer = new PrimitiveRenderer();
            primitiveRenderer.Setup(mGraphics.GraphicsDevice, kArenaWidth, kArenaHeight);
            mMTSCRenderer = new MTSCombatRenderer(primitiveRenderer);

            mMTSGame = new MTSCombatGame(2, kArenaWidth, kArenaHeight);

            SpawnVehicles();

            base.Initialize();
        }

        private GunMount MakeGunMount(float size)
        {
            Vector2[] gunArray = new Vector2[]
            {
                size * MTSCombatRenderer.GetRelativeGunMountLocation(0),
                size * MTSCombatRenderer.GetRelativeGunMountLocation(1),
                size * MTSCombatRenderer.GetRelativeGunMountLocation(2)
            };
            const float barrelReloadTime = 0.3f; 
            const float gunSpeed = 360f;
            GunMount gunMount = new GunMount(new GunData(barrelReloadTime, gunSpeed), gunArray);
            return gunMount;
        }

        private void SpawnVehicles()
        {
            const float vehicleSize = 5f;
            GunMount gunMount = MakeGunMount(vehicleSize);

            AsteroidsControlData data = new AsteroidsControlData(60f, 90f, 10f, 4f);
            VehicleDrive asteroidsDrive = AsteroidsControlsFactory.MakeDrive(data);

            VehiclePrototype asteroidsPrototype = new VehiclePrototype(vehicleSize, asteroidsDrive, AsteroidsControlsFactory.StandardConfig, gunMount);

            Vector2 position = new Vector2(kArenaWidth / 4, kArenaHeight / 4);
            Orientation2 orientation = new Orientation2(MathHelper.PiOver2);
            DynamicTransform2 initialPlacement = new DynamicTransform2(position, orientation);
            SpawnVehicle(asteroidsPrototype, initialPlacement); //TODO: This returns the assigned ID. Use it to setup proper control pipeline

            position = new Vector2(3 * kArenaWidth / 4, 3 * kArenaHeight / 4);
            orientation = new Orientation2(-MathHelper.PiOver2);
            initialPlacement = new DynamicTransform2(position, orientation);
            SpawnVehicle(asteroidsPrototype, initialPlacement);
        }

        private uint SpawnVehicle(VehiclePrototype prototype, DynamicTransform2 placement)
        {
            VehicleState initialState = new VehicleState();
            initialState.SetDriveState(placement, prototype.ControlConfig.DefaultControl);
            return mMTSGame.AddVehicle(prototype, initialState);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            mMTSGame.Tick((float)TargetElapsedTime.TotalSeconds);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            mMTSCRenderer.RenderSimState(mMTSGame.SimulationData, mMTSGame.ActiveState);

            base.Draw(gameTime);
        }
    }
}
