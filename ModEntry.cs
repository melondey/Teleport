using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Reflection;
using System;

namespace YourProjectName
{
    public class ModEntry : Mod
    {
        private List<string> friendNames = new List<string>();
        private List<MenuItem> menuItems = new List<MenuItem>();
        private const int panelWidth = 190;
        private const int panelXOffset = 160; // offset relative to the original menu
        private const int panelBoxYOffset = 88; // offset relative to the tab bar
        private const int panelItemHeight = 112;
        private const int panelItemWidth = panelWidth / 2 + 30;

        public override void Entry(IModHelper helper) {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += this.onRenderedActiveMenu;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e) {
            if (Game1.activeClickableMenu is GameMenu gameMenu) {
                foreach (var page in gameMenu.pages) {
                    if (page is SocialPage socialPage) {
                        // Re-populate list friendNames and menuItems based on current context,
                        // after opening game menu and before rendering social page tab
                        friendNames.Clear();
                        friendNames = socialPage.names.Select(name => name.ToString()).ToList<string>();

                        menuItems.Clear();
                        for (int i = 0; i < 5; i++) {
                            string label = Helper.Translation.Get("teleportButtonLabel");

                            int panelX = Game1.activeClickableMenu.xPositionOnScreen - panelXOffset;
                            int panelY = Game1.activeClickableMenu.yPositionOnScreen;

                            var textBoxTopLeft = new Point(
                            // TODO: Replace hard-coded constants 50 and 0.4f
                                panelX + panelWidth / 2 - 50 / 2,
                                (int)Math.Round(panelY + panelBoxYOffset + (0.4f + i) * panelItemHeight)
                            );

                            var background = new Rectangle(
                                panelX + panelWidth / 2 - panelItemWidth / 2,
                                panelY + panelBoxYOffset + panelItemHeight * i,
                                panelItemWidth,
                                panelItemHeight
                            );

                            menuItems.Add(new MenuItem(label, textBoxTopLeft, background));
                        }

                        break;
                    }
                }
            }
        }


        private void onRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e) {

            if (Game1.activeClickableMenu is GameMenu gameMenu) {
                if (gameMenu.currentTab == GameMenu.socialTab) {

                    int panelX = Game1.activeClickableMenu.xPositionOnScreen - panelXOffset;
                    int panelY = Game1.activeClickableMenu.yPositionOnScreen;
                    int panelHeight = Game1.activeClickableMenu.height;

                    // Draw the panel
                    Game1.drawDialogueBox(
                        panelX,
                        panelY,
                        panelWidth,
                        panelHeight,
                        false,
                        true
                    );

                    for (int i = 0; i < 5; i++) {
                        var menuItem = menuItems[i];

                        float textScale;
                        switch (Helper.Translation.Locale) {
                            case "zh-cn": 
                                textScale = 1.0f;
                                break;
                            default:
                                textScale = 0.7f;
                                break;
                        }

                        // Print button label
                        Utility.drawTextWithShadow(
                            e.SpriteBatch,
                            menuItem.label,
                            Game1.smallFont,
                            new Vector2(menuItem.textBoxTopLeft.X, menuItem.textBoxTopLeft.Y),
                            Game1.textColor,
                            textScale
                        );

                        // Draw background on hover
                        if (menuItem.background.Contains(Game1.getMouseX(ui_scale: true), Game1.getMouseY(ui_scale: true))) {
                            Game1.spriteBatch.Draw(
                                Game1.staminaRect,
                                menuItem.background,
                                Color.White * 0.5f
                            );
                        }

                        // Draw a separation line
                        if (i < 4) {
                            Game1.spriteBatch.Draw(
                                Game1.staminaRect, 
                                new Rectangle(
                                    panelX + panelWidth / 2 - panelItemWidth / 2,
                                    panelY + panelBoxYOffset + panelItemHeight * (1 + i),
                                    panelItemWidth,
                                    4
                                ), 
                                Color.SaddleBrown
                            );

                            Game1.spriteBatch.Draw(
                                Game1.staminaRect,
                                new Rectangle(
                                    panelX + panelWidth / 2 - panelItemWidth / 2,
                                    panelY + panelBoxYOffset + panelItemHeight * (1 + i) + 4,
                                    panelItemWidth,
                                    4
                                ),
                                Color.BurlyWood
                            );
                        }
                    }

                    // Draw cursor
                    if (!Game1.options.hardwareCursor) {
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            new Vector2(
                                Game1.getMouseX(ui_scale: true),
                                Game1.getMouseY(ui_scale: true)),
                            Game1.getSourceRectForStandardTileSheet(
                                Game1.mouseCursors,
                                Game1.mouseCursor,
                                16,
                                16),
                            Color.White,
                            0.0f,
                            Vector2.Zero,
                            Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f),
                            SpriteEffects.None,
                            1f
                        );
                    }
                }
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (Game1.activeClickableMenu is GameMenu gameMenu && e.Button == SButton.MouseLeft) {
                // Return unless currentTab is socialTab
                if (gameMenu.currentTab != GameMenu.socialTab)
                    return;

                SocialPage socialPage = (SocialPage)gameMenu.pages[GameMenu.socialTab];
                int slotPosition = this.Helper.Reflection.GetField<int>(socialPage, "slotPosition").GetValue();

                for (int i = 0; i < 5; i++) {
                    var menuItem = menuItems[i];

                    // Proceed if the click falls within any of the menuItems
                    if (menuItem.background.Contains(Game1.getMouseX(ui_scale: true), Game1.getMouseY(ui_scale: true))) {
                        string friendName = friendNames[i+slotPosition];
                        Point location;
                        string locationName;

                        // Name that contains digits is player ID
                        if (friendName.Any(char.IsDigit)) {
                            Farmer farmer = Game1.getFarmer(long.Parse(friendName));
                            friendName = farmer.Name;
                            locationName = farmer.currentLocation.NameOrUniqueName;
                            location = new Point(farmer.getTileX(), farmer.getTileY());
                            
                        } else {
                            /* The game does not sync real time villagers location to farmhand players.
                            Calling methods like npc.getTileLocationPoint() on farmhands' end only gets default locations.
                            The workaround here is making use of SMAPI's multiplayer API to exchange information between
                            the host and other farmhands.
                            */
                            // TODO: Check if the host has installed this mod
                            if (!Context.IsMainPlayer) {
                                // Make a request for the location of the npc identified by friendName
                                // to the host. Actual teleport will be done upon receiving response from the host, not here.
                                string message = friendName;
                                var modIds = new[] {this.ModManifest.UniqueID};
                                long[] playerIDs = null;
                                foreach (var peer in this.Helper.Multiplayer.GetConnectedPlayers()) {
                                    if (peer.IsHost) {
                                        playerIDs = new[] {peer.PlayerID};
                                    }
                                }
                                this.Helper.Multiplayer.SendMessage(message, "request", modIds, playerIDs);
                                Game1.exitActiveMenu();
                                break;
                            }

                            // Get npc location locally if current player is host
                            else {
                                NPC npc = Utility.fuzzyCharacterSearch(friendName);
                                location = npc.getTileLocationPoint();
                                locationName = npc.currentLocation.NameOrUniqueName;
                            }
                        }

                        Game1.warpFarmer(locationName, location.X, location.Y, false);
                        this.Monitor.Log($"Teleport to {friendName}@{locationName}:({location.X}, {location.Y})", LogLevel.Debug);
                        Game1.playSound("drumkit6");
                        Game1.exitActiveMenu();
                        break;
                    }
                }
            }
        }

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e) {
            if (e.FromModID == this.ModManifest.UniqueID) {
                if (e.Type == "request") {
                    string friendName = e.ReadAs<string>();
                    NPC npc = Utility.fuzzyCharacterSearch(friendName);
                    Point location = npc.getTileLocationPoint();
                    string locationName = npc.currentLocation.NameOrUniqueName;
                    var message = new LocationResponse(friendName, locationName, location);

                    var modIDs = new[] {e.FromModID};
                    var playerIds = new[] {e.FromPlayerID};

                    this.Helper.Multiplayer.SendMessage(message, "response", modIDs, playerIds);
                } else if (e.Type == "response") {
                    var response = e.ReadAs<LocationResponse>();
                    string friendName = response.npcName;
                    string locationName = response.locationName;
                    int x = response.location.X;
                    int y = response.location.Y;

                    Game1.warpFarmer(locationName, x, y, false);
                    this.Monitor.Log($"Teleport to {friendName}@{locationName}:({x}, {y})", LogLevel.Debug);
                    Game1.playSound("drumkit6");
                }
            }
        }
    }

    class LocationResponse {
        public string npcName;
        public string locationName;
        public Point location;
        public LocationResponse(string npcName, string locationName, Point location){
            this.npcName = npcName;
            this.locationName = locationName;
            this.location = location;
        }
    }

    class MenuItem {
        public string label;
        public Point textBoxTopLeft;
        public Rectangle background;
        public MenuItem(string label, Point textBoxTopLeft, Rectangle background) {
            this.label = label;
            this.textBoxTopLeft = textBoxTopLeft;
            this.background = background;
        }
    }
}