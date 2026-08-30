## Strings for the drop-down window to manage your active account

account-drop-down-none-selected = No account selected
account-drop-down-not-logged-in = Not logged in
account-drop-down-log-out = Log out
account-drop-down-log-out-of = Log out of { $name }
account-drop-down-switch-account = Switch account:
account-drop-down-select-account = Select account:
account-drop-down-add-account = Add account

## Localization for the "add favorite server" dialog window

add-favorite-window-title = Add Favorite Server
add-favorite-window-address-invalid = Address is invalid
add-favorite-window-label-name = Name:
add-favorite-window-label-address = Address:
# 'Example' name shown as a watermark in the name input box
add-favorite-window-example-name = Honk Station
add-favorite-window-button-add = Add

## Strings for the "connecting" menu that appears when connecting to a server.

connecting-title-connecting = Connecting…
connecting-title-content-bundle = Loading…
connecting-cancel = Cancel
connecting-status-none = Starting connection…
connecting-status-update-error =
    There was an error while downloading server content. If this persists try some of the following:
    - Try connecting to another game server to see if the problem persists.
    - Try disabling or enabling software such as VPNs, if you have any.

    If you are still having issues, first try contacting the server you are attempting to join before asking for support on the Official Space Station 14 Discord or Forums.

    Technical error: { $err }
connecting-status-update-error-no-engine-for-platform = This server is using an older game engine version that does not support your current platform. Please try another server or inform the server staff about this and try again later.
connecting-status-update-error-no-module-for-platform = This server requires additional functionality that is not yet supported on your current platform. Please try another server or inform the server staff about this and try again later.
connecting-status-update-error-unknown = Unknown
connecting-status-updating = Updating: { $status }
connecting-status-connecting = Fetching connection info from server…
connecting-status-connection-failed = Failed to connect to server!
connecting-status-starting-client = Starting client…
connecting-status-not-a-content-bundle = File is not a valid content bundle!
connecting-status-client-crashed = Client seems to have crashed while starting. If this persists, please ask on Discord or GitHub for support.
connecting-update-status-checking-client-update = Checking for server content update…
connecting-update-status-downloading-engine = Downloading server content…
connecting-update-status-downloading-content = Downloading server content…
connecting-update-status-fetching-manifest = Fetching server manifest…
connecting-update-status-verifying = Verifying download integrity…
connecting-update-status-culling-engine = Clearing old content…
connecting-update-status-culling-content = Clearing old server content…
connecting-update-status-ready = Update done!
connecting-update-status-checking-engine-modules = Checking for additional dependencies…
connecting-update-status-downloading-engine-modules = Downloading extra dependencies…
connecting-update-status-committing-download = Synchronizing to disk…
connecting-update-status-loading-into-db = Storing assets in database…
connecting-update-status-loading-content-bundle = Loading content bundle…
connecting-update-status-unknown = You shouldn't see this

connecting-privacy-policy-text = This server requires that you accept its privacy policy before connecting.
connecting-privacy-policy-text-version-changed = This server has updated its privacy policy since the last time you played. You must accept the new version before connecting.
connecting-privacy-policy-view = View privacy policy
connecting-privacy-policy-accept = Accept (continue)
connecting-privacy-policy-decline = Decline (disconnect)

## Strings for the "direct connect" dialog window.

direct-connect-title = Direct Connect
direct-connect-text = Enter server address to connect:
direct-connect-connect = Connect
direct-connect-address-invalid = Address is invalid

## Strings for the "hub settings" dialog window.

hub-settings-title = Hub Settings
hub-settings-button-done = Done
hub-settings-button-cancel = Cancel
hub-settings-button-reset = Reset
hub-settings-button-reset-tooltip = Reset to default settings
hub-settings-button-add-tooltip = Add hub
hub-settings-button-test = Test Hubs
hub-settings-button-test-tooltip = Measure latency to all configured hubs in list
hub-settings-button-remove-tooltip = Remove hub
hub-settings-button-increase-priority-tooltip = Increase priority
hub-settings-button-decrease-priority-tooltip = Decrease priority
hub-settings-explanation = Here you can add extra hubs to fetch game servers from. You should only add hubs that you trust, as they can 'spoof' game servers from other hubs. The order of the hubs matters; if two hubs advertise the same game server the hub with the higher priority (higher in the list) will take precedence.
hub-settings-heading-default = Default
hub-settings-heading-custom = Custom
hub-settings-warning-invalid = Invalid hub (don't forget http(s)://)
hub-settings-warning-duplicate = Duplicate hubs

## Strings for the login screen

login-log-launcher = Log Launcher

## Error messages for login

login-error-invalid-credentials = Invalid login credentials
login-error-account-unconfirmed = The email address for this account still needs to be confirmed. Please confirm your email address before trying to log in
login-error-account-2fa-required = 2-factor authentication required
login-error-account-2fa-invalid = 2-factor authentication code invalid
login-error-account-account-locked = Account has been locked. Please contact us if you believe this to be in error.
login-error-unknown = Unknown error
login-errors-button-ok = Ok

## Strings for 2FA login

login-2fa-title = 2-factor authentication required
login-2fa-message = Please enter the authentication code from your app.
login-2fa-input-watermark = Authentication code
login-2fa-button-confirm = Confirm
login-2fa-button-recovery-code = Recovery code
login-2fa-button-cancel = Cancel

## Strings for the "login expired" view on login

login-expired-title = Login expired
login-expired-message =
    The session for this account has expired.
    Please re-enter your password.
login-expired-password-watermark = Password
login-expired-button-log-in = Log in
login-expired-button-log-out = Log out
login-expired-button-forgot-password = Forgot your password?

## Strings for the "forgot password" view on login

login-forgot-title = Forgot password?
login-forgot-message = If you've forgotten your password, you can enter the email address associated with your account here to receive a reset link.
login-forgot-email-watermark = Your email address
login-forgot-button-submit = Submit
login-forgot-button-back = Back to login
login-forgot-busy-sending = Sending email…
login-forgot-success-title = Reset email sent
login-forgot-success-message = A reset link has been sent to your email address.
login-forgot-error = Error

## Strings for the "login" view on login

login-login-title = Log in
login-login-auth-server-changed = Auth server has changed
login-login-username-watermark = Username or email
login-login-password-watermark = Password
login-login-show-password = Show Password
login-login-button-log-in = Log in
login-login-button-forgot = Forgot your password?
login-login-button-resend = Resend email confirmation
login-login-button-register = Don't have an account? Register!
login-login-busy-logging-in = Logging in…
login-login-error-title = Unable to log in

## Strings for the "register confirmation" view on login

login-confirmation-confirmation-title = Register confirmation
login-confirmation-confirmation-message = Please check your email to confirm your account. Once you have confirmed your account, press the button below to log in.
login-confirmation-button-confirm = I have confirmed my account
login-confirmation-button-cancel = Cancel
login-confirmation-busy = Logging in…

## Strings for the general main window layout of the launcher

main-window-title = Space Station 14 Launcher
main-window-header-link-discord = Discord
main-window-header-link-website = Website
main-window-out-of-date = Launcher out of date
main-window-out-of-date-desc =
    This launcher is out of date.
    Please download a new version from our website.
main-window-out-of-date-desc-steam =
    This launcher is out of date.
    Please allow Steam to update the game.
main-window-out-of-date-exit = Exit
main-window-out-of-date-download-manual = Download (manual)
main-window-early-access-title = Heads up!
main-window-early-access-desc = Space Station 14 is still very much in alpha. We hope you like what you see, and maybe even stick around, but make sure to keep your expectations modest for the time being.
main-window-early-access-accept = Got it!
main-window-intel-degrade-title = Intel 13th/14th Generation CPU detected.
main-window-intel-degrade-desc =
    The Intel 13th/14th generation CPUs are known to silently degrade permanently and die due to a microcode bug by Intel. We sadly can't tell if you are currently affected by this bug, so this warning appears for all users with these CPUs.

    We STRONGLY encourage you to update your motherboard's BIOS to the latest version to ensure prevention of further damage. If you are having stability issues/failing to start the game, downclock your CPU to get it stable again and use your warranty to ask about getting it replaced.

    We are not responsible to help with any issues that may arise from affected processors, unless you took the precautions and are sure your CPU is stable. This message will not appear again after you accept it.
main-window-intel-degrade-accept = I understand and have taken the necessary precautions.
main-window-rosetta-title = You are running the game using Rosetta 2!
main-window-rosetta-desc =
    You seem to be on an Apple Silicon Mac and are running the game using Rosetta 2. You may enjoy better performance and battery life by running the game natively.

    To do this, right click the launcher in Finder, select "Get Info" and uncheck "Open using Rosetta". After that, restart the launcher.

    If you are intentionally running the game using Rosetta 2, you can dismiss this message and it will not appear again. Although if you are doing this in an attempt to fix a problem, please make a bug report.
main-window-rosetta-accept = Continue
main-window-drag-drop-prompt = Drop to run game
main-window-busy-checking-update = Checking for launcher update…
main-window-busy-checking-login-status = Refreshing login status…
main-window-busy-checking-account-status = Checking account status
main-window-error-connecting-auth-server = Error connecting to authentication server
main-window-error-unknown = Unknown error occurred
main-window-auth-override-title = The authentication server URL has changed
main-window-auth-override-desc =
     If you don't remember changing this, it's possible someone malicious may be trying to snoop on your credentials. By closing this popup, you agree you are responsible for your own security, and will not be provided support.
main-window-auth-override-acknowledge = I acknowledge

## Long region names for server tag filters (shown in tooltip)

region-africa-central = Africa Central
region-africa-north = Africa North
region-africa-south = Africa South
region-antarctica = Antarctica
region-asia-east = Asia East
region-asia-north = Asia North
region-asia-south-east = Asia South East
region-central-america = Central America
region-europe-east = Europe East
region-europe-west = Europe West
region-greenland = Greenland
region-india = India
region-middle-east = Middle East
region-the-moon = The Moon
region-north-america-central = North America Central
region-north-america-east = North America East
region-north-america-west = North America West
region-oceania = Oceania
region-south-america-east = South America East
region-south-america-south = South America South
region-south-america-west = South America West

## Short region names for server tag filters (shown in filter check box)

region-short-africa-central = Africa Central
region-short-africa-north = Africa North
region-short-africa-south = Africa South
region-short-antarctica = Antarctica
region-short-asia-east = Asia East
region-short-asia-north = Asia North
region-short-asia-south-east = Asia South East
region-short-central-america = Central America
region-short-europe-east = Europe East
region-short-europe-west = Europe West
region-short-greenland = Greenland
region-short-india = India
region-short-middle-east = Middle East
region-short-the-moon = The Moon
region-short-north-america-central = NA Central
region-short-north-america-east = NA East
region-short-north-america-west = NA West
region-short-oceania = Oceania
region-short-south-america-east = SA East
region-short-south-america-south = SA South
region-short-south-america-west = SA West

## Strings for the "servers" tab

tab-servers-title = Servers
tab-servers-refresh = Refresh
filters = Filters ({ $filteredServers } / { $totalServers })
tab-servers-search-watermark = Search For Servers…
tab-servers-table-players = Players
tab-servers-table-name = Server Name
tab-servers-table-round-time = Time
tab-servers-list-status-error = There was an error fetching the master server lists. Maybe try refreshing?
tab-servers-list-status-partial-error = Failed to fetch some of the server lists. Ensure your hub configuration is correct and try refreshing.
tab-servers-list-status-updating-master = Fetching master server list…
tab-servers-list-status-none-filtered = No servers match your search or filter settings.
tab-servers-list-status-none = There are no public servers. Ensure your hub configuration is correct.

## Strings for the server filters menu

filters-title = Filters
filters-title-language = Language
filters-title-region = Region
filters-title-rp = Role-play level
filters-title-player-count = Player count
filters-title-18 = 18+
filters-title-hub = Hub
filters-18-yes = Yes
filters-18-yes-desc = Yes
filters-18-no = No
filters-18-no-desc = No
filters-player-count-hide-empty = Hide empty
filters-player-count-hide-empty-desc = Servers with no players will not be shown
filters-player-count-hide-full = Hide full
filters-player-count-hide-full-desc = Servers that are full will not be shown
filters-player-count-minimum = Minimum:
filters-player-count-minimum-desc = Servers with less players will not be shown
filters-player-count-maximum = Maximum:
filters-player-count-maximum-desc = Servers with more players will not be shown
filters-unspecified-desc = Unspecified
filters-unspecified = Unspecified

## Server roleplay levels for the filters menu

filters-rp-none = None
filters-rp-none-desc = None
filters-rp-low = Low
filters-rp-low-desc = Low
filters-rp-medium = Medium
filters-rp-medium-desc = Medium
filters-rp-high = High
filters-rp-high-desc = High

## Strings for entries in the server list (including home page)

server-entry-connect = Connect
server-entry-add-favorite = Favorite
server-entry-remove-favorite = Unfavorite
server-entry-offline = OFFLINE
server-entry-player-count =
    { $players } / { $max ->
        [0] ∞
       *[1] { $max }
    }
server-entry-round-time = { $hours ->
 [0] { $mins }M
*[1] { $hours }H { $mins }M
}
server-entry-fetching = Fetching…
server-entry-description-offline = Unable to contact server
server-entry-description-fetching = Fetching server status…
server-entry-description-error = Error while fetching server description
server-entry-description-none = No server description provided
server-entry-status-lobby = Lobby
server-fetched-from-hub = Fetched from { $hub }
server-entry-raise = Raise to top

## Strings for the "Development" tab
## These aren't shown to users so they're not very important

tab-development-title = { "[" }DEV]
tab-development-title-override = { "[" }DEV (override active!!!)]
tab-development-disable-signing = Disable Engine Signature Checks
tab-development-disable-signing-desc = { "[" }DEV ONLY] Disables verification of engine signatures. DO NOT ENABLE UNLESS YOU KNOW EXACTLY WHAT YOU'RE DOING.
tab-development-enable-engine-override = Enable engine override
tab-development-enable-engine-override-desc = Override path to load engine zips from (release/ in RobustToolbox)
tab-development-launch-card = Launch Parameters & CLI Arguments
tab-development-launch-args-title = Custom client process launch arguments:
tab-development-launch-args-watermark = e.g. --connect-address ss14://localhost:1212 --cvar net.fakelag=50
tab-development-launch-args-desc = Arguments will be appended directly to the Robust executable arguments.
tab-development-log-level = Log level:
tab-development-uncapped-fps = Unlock FPS and disable VSync (--cvar display.vsync=false display.max_fps=0)
tab-development-graphics-card = Graphics, Display & In-Game Overlays
tab-development-graphics-backend = Graphics backend:
tab-development-display-mode = Window mode:
tab-development-fps-overlay = Show FPS & FrameTime overlay (+showtime)
tab-development-net-graph = NetGraph network overlay (+netgraph)
tab-development-open-console = Open dev console on start (+toggleconsole)
tab-development-physics-debug = Show physics & hitboxes (physics.debug)
tab-development-show-lightmap = Show lighting & lightmap grid (light.debug)
tab-development-show-entity-bounds = Show entity bounding boxes (debug.entity_bounds)
tab-development-mute-audio = Mute all audio on launch (audio.master_volume=0)
tab-development-lowlevel-card = Low-Level CLR & Engine Tuning
tab-development-dynamic-pgo = Dynamic PGO (Profile-Guided Optimization)
tab-development-gc-limit = GC Heap Limit (MB, 0 = no limit):
tab-development-crash-dumps = Generate crash dumps (.dmp) on crash
tab-development-render-validation = Render API validation layers (Vulkan / Mesa Debug)
tab-development-fast-threadpool = Fast CLR ThreadPool (16 Min Workers)
tab-development-loh-compact = Aggressive Large Object Heap Compaction (LOH Compact)
tab-development-strict-diagnostics = Break execution on any unhandled error (Strict Break-on-Crash)
tab-development-gc-no-affinitize = Non-affinitized GC threads (DOTNET_GCNoAffinitize)
tab-development-network-card = Network Emulation & Environment
tab-development-simulated-ping = Simulated Ping (ms):
tab-development-simulated-jitter = Simulated Jitter (ms):
tab-development-simulated-loss = Packet loss (%):
tab-development-disable-net-compression = Disable network packet compression (net.compression=false)
tab-development-custom-env = Custom environment variables (KEY=VALUE separated by semicolon or newline):
tab-development-maintenance-card = Directories & Maintenance
tab-development-open-user-data = Open user data folder
tab-development-open-logs = Open logs folder
tab-development-clear-engines = Clear Robust engines
tab-development-clear-servers = Clear server content cache
tab-development-clear-logs = Clear log files

## Strings for the "home" tab

tab-home-title = Home
tab-home-favorite-servers = Favorite Servers
tab-home-add-favorite = Add favorite
tab-home-refresh = Refresh
tab-home-direct-connect = Direct connect to server
tab-home-connection-history = Connection History
tab-home-run-content-bundle = Run content bundle/replay
tab-home-go-to-servers-tab = Go to the servers tab
tab-home-favorites-guide = Mark servers as favorite for easy access here

## Strings for Server History Dialog
server-history-title = Connection History
server-history-search-watermark = Search history by name or address…
server-history-clear = Clear History
server-history-empty = No connection history yet.
server-history-connect = Connect
server-history-copy = Copy Address
server-history-add-favorite = Add to Favorites
server-history-remove = Remove
server-history-close = Close

## Strings for the "news" tab

tab-news-title = News
tab-news-recent-news = Recent News:
tab-news-pulling-news = Pulling news…

## Strings for the "options" tab

tab-options-title = Options
tab-options-flip = { "*" }flip
tab-options-clear-engines = Clear installed engines
tab-options-clear-content = Clear installed server content
tab-options-clear-content-close-client = Close running clients first
tab-options-open-log-directory = Open log directory
tab-options-account-settings = Account Settings
tab-options-account-settings-desc = You can manage your account settings, such as changing email or password, through our website.
tab-options-compatibility-mode = Compatibility Mode
tab-options-compatibility-mode-desc = This forces the game to use a different graphics backend, which is less likely to suffer from driver bugs. Try this if you are experiencing graphical issues or crashes.
tab-options-log-client = Log Client
tab-options-log-client-desc = Enables logging of any game client output. Useful for developers.
tab-options-log-launcher = Log Launcher
tab-options-log-launcher-desc = Enables logging of the launcher. Useful for developers. (requires launcher restart)
tab-options-section-appearance = Appearance & Customization
tab-options-section-network = Network & Proxy
tab-options-section-performance = Performance & Accounts
tab-options-section-system = System & Hubs

tab-options-doh = DNS-over-HTTPS (DoH Resolver)
tab-options-doh-desc = Uses secure encrypted DNS (Cloudflare / Google 1.1.1.1) for reliable hub connectivity bypassing ISP outages.
tab-options-proxy-settings = Proxy Settings
tab-options-proxy-settings-desc = Configure SOCKS5 or HTTP/HTTPS proxy to route game and launcher traffic.
tab-options-proxy-button = Proxy Settings (SOCKS5 / HTTP)

tab-options-low-latency-net = Low-Latency Sockets (Inline Completions)
tab-options-low-latency-net-desc = Reduces networking delay by completing socket tasks synchronously where possible.
tab-options-disable-diagnostics = Disable Runtime Diagnostics Overhead
tab-options-disable-diagnostics-desc = Disables internal runtime event pipes and tracing for slightly better CPU efficiency.
tab-options-low-pause-gc = Background Low-Pause GC
tab-options-low-pause-gc-desc = Reduces micro-stutters by running memory garbage collection in parallel background threads.
tab-options-smart-cache-cleaner = Smart Cache Cleaner
tab-options-smart-cache-cleaner-desc = Automatically removes outdated server and engine builds older than 14 days to save disk space.
tab-options-fast-launch = Background Update Preloading (Fast Launch)
tab-options-fast-launch-desc = Checks and pre-downloads updates for favorite servers in background for instant launch.

tab-options-log-viewer = Log & Crash Inspector
tab-options-local-builds = Local Builds Manager
tab-options-smart-clean = Smart Cache Cleanup

tab-options-show-news-tab = Show News Tab
tab-options-show-news-tab-desc = Displays official news feed and updates for Space Station 14.
tab-options-show-replays-tab = Show Replays Tab
tab-options-show-replays-tab-desc = Displays round recording manager, metadata viewer, and replay player.
tab-options-show-dev-tab = Show [DEV] Developer Tab
tab-options-show-dev-tab-desc = Displays low-level engine debugging tools, network emulation, and launch options.

tab-options-verbose-launcher-logging = Verbose Launcher Logging
tab-options-verbose-launcher-logging-desc = For when the developers are *very* stumped with your problem. (requires launcher restart)
tab-options-seasonal-branding = Seasonal Branding
tab-options-seasonal-branding-desc = Whatever temporally relevant icons and logos we can come up with.
tab-options-disable-signing = Disable Engine Signature Checks
tab-options-disable-signing-desc = { "[" }DEV ONLY] Disables verification of engine signatures. DO NOT ENABLE UNLESS YOU KNOW EXACTLY WHAT YOU'RE DOING.
tab-options-hub-settings = Hub Settings
tab-options-hub-settings-desc = Configure master server hubs to discover community servers.
tab-options-launcher-customizer = Launcher Customization
tab-options-launcher-customizer-desc = Customize background wallpaper, glassmorphism tint, custom logo, and button names.
launcher-customizer-bg-heading = Background Wallpaper (PNG / JPG / WebP)
launcher-customizer-glass-opacity = Glass Tint / Opacity:
launcher-customizer-logo-heading = Custom Game Logo (PNG)
launcher-customizer-tab-names-heading = Custom Tab Names
launcher-customizer-tab-home = Home Tab:
launcher-customizer-tab-servers = Servers Tab:
launcher-customizer-tab-news = News Tab:
launcher-customizer-tab-options = Options Tab:
launcher-customizer-browse = Browse...
tab-options-open-user-data = Open Data Directory
tab-options-multi-accounts = Multi-Account Mode
tab-options-multi-accounts-desc = Allows saving and switching between multiple Space Station 14 accounts in the launcher.
tab-options-pgo-optimizations = Multi-Thread PGO JIT Optimization
tab-options-pgo-optimizations-desc = Enables tiered Profile-Guided Optimization for the game runtime, speeding up startup and in-game performance.
tab-options-server-gc = Server GC Mode for Game Client
tab-options-server-gc-desc = Uses multi-threaded Server Garbage Collector for reduced frame drops and smoother gameplay.
tab-options-fast-ping = Fast TCP Ping
tab-options-fast-ping-desc = Measures real TCP connection ping for servers in parallel.
tab-options-process-priority = High Game Process Priority
tab-options-process-priority-desc = Automatically elevates game process priority (AboveNormal) to prevent FPS drops from background applications.
tab-options-dedicated-gpu = Force Dedicated GPU Mode
tab-options-dedicated-gpu-desc = Forces the game to use discrete high-performance GPU (NVIDIA PRIME / AMD DRI) on hybrid systems.
tab-options-max-jit = Hardware Vectorization & Deep JIT
tab-options-max-jit-desc = Unlocks maximum CPU SIMD intrinsics (AVX2/FMA), non-conservative GC tuning, and zero JIT minimization.

## Strings for Proxy Settings Dialog
proxy-dialog-title = Proxy Settings (SOCKS5 / HTTP)
proxy-dialog-enable = Enable network proxy
proxy-dialog-enable-desc = Routes game and launcher network traffic through the specified proxy server.
proxy-dialog-section-params = Proxy Parameters
proxy-dialog-protocol = Protocol:
proxy-dialog-host = Host address (IP / Domain):
proxy-dialog-port = Port:
proxy-dialog-section-auth = Authentication (Optional)
proxy-dialog-username = Username:
proxy-dialog-password = Password:
proxy-dialog-section-scope = Routing Scope
proxy-dialog-scope-game = Apply to game client (ALL_PROXY / HTTP_PROXY)
proxy-dialog-scope-launcher = Apply to launcher requests (Hub and Auth)
proxy-dialog-section-test = Diagnostics & Test
proxy-dialog-test-button = Test Connection
proxy-dialog-save = Save
proxy-dialog-cancel = Cancel
server-entry-copy-address = Copy Address
tab-options-desc-incompatible = This option is incompatible with your platform and has been disabled.

## For the language selection menu.

# Text on the button that opens the menu.
language-selector-label = Language
# "Save" button.
language-selector-save = Save
# "Cancel" button.
language-selector-cancel = Cancel
language-selector-help-translate = Want to help translate? You can!
language-selector-system-language = System language ({ $languageName })
# Used for contents of each language button.
language-selector-language = { $languageName } ({ $englishName })

## Miscellaneous

# Generic "Done!" message used for some buttons.
button-done = Done!

## Panic bunker tag
server-entry-bunker-tag = [Bunker]
server-entry-panic-bunker-active = Panic bunker active
server-entry-panic-bunker-account-age-days = Account age: { $days } d.
server-entry-panic-bunker-account-age-mins = Account age: { $mins } m.
server-entry-panic-bunker-overall-hours = Playtime: { $hours } h.

## Strings for News Tab
tab-news-search-watermark = Search news by title or description…
tab-news-refresh = 🔄 Refresh
tab-news-refresh-tooltip = Refresh news feed
tab-news-empty-title = No news to display
tab-news-empty-desc = News feed is empty or failed to load data from official news server.
tab-news-retry = Retry loading
tab-news-read-more = Read ↗
tab-news-read-more-tooltip = Open article on official website

## Strings for Replays Tab
tab-replays-title = Replays
tab-replays-search-watermark = Search round recordings by title, map or date…
tab-replays-browse-zip = 📂 Select .zip
tab-replays-open-folder = 📁 Folder
tab-replays-refresh-tooltip = Refresh list
tab-replays-empty-title = No saved replays
tab-replays-empty-desc = Round recordings (.zip) are saved in launcher's replays folder or can be opened directly via the file picker.
tab-replays-select-file = Select replay file to watch
tab-replays-play = ▶ Watch
tab-replays-delete-tooltip = Delete recording
tab-replays-sort-date-desc = Date (Newest first)
tab-replays-sort-date-asc = Date (Oldest first)
tab-replays-sort-size-desc = Size (Largest first)
tab-replays-sort-name-asc = Title (A-Z)

## Strings for Launcher Customizer Dialog
launcher-customizer-tab-visuals = Appearance & Palette
launcher-customizer-tab-docking = Tabs & Placement
launcher-customizer-tab-sandbox = Studio Script Sandbox
launcher-customizer-presets-title = Style Presets
launcher-customizer-preset-classic = Classic SS14
launcher-customizer-preset-cyberpunk = Cyberpunk
launcher-customizer-preset-syndicate = Syndicate
launcher-customizer-preset-solar = Solar Gold
launcher-customizer-preset-deep-space = Deep Space
launcher-customizer-preset-matrix = Matrix
launcher-customizer-preset-monochrome = Monochrome
launcher-customizer-clear-bg-tooltip = Clear background
launcher-customizer-bg-watermark = Select PNG, JPG, GIF, WebP or video (MP4, WebM)…
launcher-customizer-preview-glass = Background glass effect preview
launcher-customizer-clear-logo-tooltip = Clear logo
launcher-customizer-logo-watermark = Select PNG logo file…
launcher-customizer-colors-title = Interface Colors Fine-Tuning
launcher-customizer-color-accent = Accent color (Gold):
launcher-customizer-color-button = Button color (Buttons):
launcher-customizer-color-tab-selected = Active tab color:
launcher-customizer-color-text = Text color:
launcher-customizer-color-popup = Cards & popups background:
launcher-customizer-window-title = Launcher window title:
launcher-customizer-window-title-watermark = Default: Space Station 14 Launcher
launcher-customizer-font-scale = Font Scale:
launcher-customizer-click-vfx = Button click ripple & spring animations (VFX)
launcher-customizer-dock-placement-title = Navigation Tab Placement & Docking
launcher-customizer-dock-placement-desc = Choose which side of the launcher the navigation tab strip is docked to:
launcher-customizer-dock-placement-label = Tab Strip Docking:
launcher-customizer-dock-placement-hint = Supports docking Top, Bottom, Left, and Right.
launcher-customizer-tab-replays = Replays Tab:
launcher-customizer-sandbox-title = Interactive Style & Script Engine
launcher-customizer-sandbox-templates = Quick code templates:
launcher-customizer-template-neon = Neon
launcher-customizer-template-matrix = Matrix
launcher-customizer-template-minimalist = Minimalist
launcher-customizer-sandbox-execute = ▶ Execute & apply script
launcher-customizer-sandbox-watermark = // Enter commands or paste a template above…&#x0a;Accent = #00F2FE&#x0a;Button = #2D1B4E&#x0a;TabSelected = #FF007F&#x0a;Opacity = 0.85&#x0a;Servers = Stations
launcher-customizer-sandbox-cheatsheet-title = Commands reference:

## Strings for Log Viewer Dialog
log-viewer-title = Logs & Crash Viewer
log-viewer-file = Log File:
log-viewer-search-watermark = Search / filter errors (error, exception, crash)…
log-viewer-refresh = 🔄 Refresh
log-viewer-open-folder = 📁 Logs Folder
log-viewer-copy = 📋 Copy Log / Stacktrace
log-viewer-close = Close

## Strings for Local Builds Dialog
local-builds-title = Local Builds & Forks Manager
local-builds-close = Close
local-builds-section-add = ➕ Add Local Build or Content Bundle
local-builds-name = Name:
local-builds-name-watermark = e.g. Space Stories Local / SS14 Master
local-builds-path = Build File:
local-builds-path-watermark = Path to .zip bundle or executable
local-builds-browse = 📂 Browse…
local-builds-add-button = ➕ Add Build to List
local-builds-section-saved = 📁 Saved Builds
local-builds-launch = ▶ Launch
local-builds-delete = 🗑 Delete

## Strings for Account Info Dialog
account-info-title = Account Information
account-info-close = Close
account-info-open-web = Open Account Page ↗
account-info-section-profile = 👤 Profile & Identity
account-info-username = Username:
account-info-user-id = User ID:
account-info-copy = Copy
account-info-copy-id-tooltip = Copy User ID to clipboard
account-info-email = Email:
account-info-session-status = Session Status:
account-info-section-security = 🔑 Security & Password
account-info-password-storage = Password Storage:
account-info-token-expires = Token Expiration:
account-info-change-password = Change Password ↗
account-info-2fa = 2FA Settings ↗
account-info-section-device = 💻 Device & Hardware ID (HWID)
account-info-hwid = Hardware ID:
account-info-copy-hwid-tooltip = Copy HWID to clipboard
account-info-system = OS & System:
account-info-total-playtime = Total Playtime:
account-info-copy-all-diag = Copy Full Diagnostics
account-info-not-logged-in = Not logged in
account-info-password-protected = Password protected (JWT Token)
account-info-email-linked = Linked to SS14 profile
account-info-status-active = Active (Logged in)
account-info-status-expired = Expired (Re-login required)
account-info-status-unknown = Unknown
account-info-token-permanent = Permanent / Until logout
account-info-guest = Guest Mode / Not logged in
account-info-none = None
proxy-dialog-testing = Testing connection via proxy…
proxy-dialog-test-specify-host = ⚠ Please specify the proxy host address!
proxy-dialog-test-success = ✓ Success! Ping via { $type }: { $ping } ms
proxy-dialog-test-error-status = ⚠ Proxy responded with status: { $status } { $reason } ({ $ping } ms)
proxy-dialog-test-error = ⚠ Failed to connect to proxy: { $error }
tab-dev-cleared-engines = ✓ All Robust engine versions successfully removed.
tab-dev-error-engines = ⚠ Failed to clear engines: { $error }
tab-dev-cleared-content = ✓ Server content cache and build data cleared.
tab-dev-error-content = ⚠ Failed to clear content: { $error }
tab-dev-cleared-logs = ✓ Log files cleared.
tab-dev-error-logs = ⚠ Failed to clear logs: { $error }
tab-dev-opened-user-data = ✓ User data directory opened.
tab-dev-error-folder = ⚠ Failed to open directory: { $error }
tab-dev-opened-logs = ✓ Logs directory opened.
tab-dev-error-logs-folder = ⚠ Failed to open logs directory: { $error }
local-builds-badge-zip = 📦 ZIP Bundle
local-builds-badge-exe = ⚙️ Executable
local-builds-added-date = Added: { $date }
log-viewer-no-files = No log files found.
log-viewer-error-read-dir = Error reading logs directory: { $error }
log-viewer-loaded = Loaded { $lines } lines from { $file }
log-viewer-error-read-file = Failed to read { $file }: { $error }
log-viewer-copied = Copied to clipboard!
log-viewer-error-copy = Copy error: { $error }
log-viewer-error-open-dir = Failed to open directory: { $error }
local-builds-error-load = Error loading list: { $error }
local-builds-error-save = Error saving: { $error }
local-builds-picker-title = Select a build file or content bundle (.zip / executable)
local-builds-picker-all = All supported files
local-builds-picker-bundles = SS14 content bundles (*.zip)
local-builds-error-pick = Error selecting file: { $error }
local-builds-validation-empty = Please specify a name and a path to the build file.
local-builds-validation-not-found = Specified file or folder does not exist!
local-builds-added = Build "{ $name }" added successfully!
local-builds-removed = Build "{ $name }" removed.
local-builds-launching = Launching "{ $name }"...
local-builds-launched = Process "{ $name }" started!
local-builds-not-found = Build file not found at specified path.
local-builds-error-launch = Launch error: { $error }
customizer-script-empty = ⚠ Script is empty.
customizer-script-done = ✓ Done: applied { $applied } rules{ $errors }.
customizer-script-errors-suffix = , skipped { $skipped } lines
customizer-script-ready = Ready.
customizer-script-reset = Reset to original theme.
replays-picker-title = Select a Space Station 14 replay file (.zip)

## Self-Updater & About Section
tab-options-section-about = About Launcher
tab-options-launcher-version = Launcher Version:
tab-options-check-updates = Check for Updates
tab-options-checking-updates = Checking for updates...
tab-options-update-available-text = New version available: { $version }!
tab-options-up-to-date-text = Launcher is up to date.
tab-options-update-error = Update check failed: { $error }
tab-options-update-now-button = Update Now

launcher-update-available-title = Launcher Update Available
launcher-update-available-message = A new version of SS14 Launcher is available: { $version }
launcher-update-current-version = Current version: { $version }
launcher-update-btn-update-now = Update Now
launcher-update-btn-later = Later
launcher-update-btn-skip = Skip Version
launcher-update-downloading-title = Updating Launcher...
launcher-update-status-downloading = Downloading update package...
launcher-update-status-installing = Installing update and restarting...
launcher-update-error-failed = Update failed: { $error }

## Smart Ranking & Desktop Shortcut
filters-title-smart-ranking = Smart Ranking
filters-recommended = Recommended
filters-recommended-desc = Show only active, low-latency, and high-quality servers

tab-options-create-desktop-shortcut = Create Desktop Shortcut
tab-options-desktop-shortcut-success = Desktop and Application Menu shortcuts created!
tab-options-desktop-shortcut-error = Failed to create shortcut: { $error }

## Playtime Tracking, Slot Notifier & Themes
tab-options-track-playtime = Track Playtime
tab-options-track-playtime-desc = Keep track of time spent on servers and display played hours statistics.
tab-options-enable-slot-notifier = Player Slot Notifications
tab-options-enable-slot-notifier-desc = Display slot monitoring buttons on full servers and notify when a slot opens up.

server-entry-playtime-hours = { $hours }h { $mins }m
server-entry-playtime-mins = { $mins }m
server-entry-playtime-tooltip = Played time: { $time }
server-entry-slot-watcher-active = Slot monitoring active. You will be notified when space opens up.
server-entry-slot-watcher-inactive = Notify when a player slot becomes available.

notification-slot-available-title = Player Slot Available!
notification-slot-available-desc = A free player slot is now available on "{ $server }" ({ $players }/{ $max }).
tab-options-test-notification = Test Notification
notification-test-title = Space Station 14 Launcher
notification-test-desc = Desktop notifications are working properly!

server-entry-panic-bunker-details-title = Server has Panic Bunker protection enabled:
server-entry-panic-bunker-req-age-days = Minimum required account age: { $days } days
server-entry-panic-bunker-req-age-mins = Minimum required account age: { $mins } minutes
server-entry-panic-bunker-req-playtime = Minimum required overall playtime: { $hours } hours

launcher-customizer-export = Export
launcher-customizer-import = Import
customizer-export-success = Theme code copied to clipboard!
customizer-import-success = Theme imported successfully from clipboard!

## Content Database Integrity
tab-options-verify-db = Verify Database Integrity
tab-options-db-integrity-ok = Integrity OK! (Cleaned: { $cleaned })
tab-options-db-integrity-error = Database integrity errors detected!

## Extended Development Tab
tab-dev-force-scalar-search = Scalar Search (Disable SIMD)
tab-dev-diagnostics-card = Diagnostics & Algorithm Benchmarks
tab-dev-btn-benchmark = Algorithm Benchmark
tab-dev-btn-sysinfo = System Diagnostics
tab-dev-btn-netdiag = Network & CDN Diagnostics
tab-dev-btn-force-gc = Force Garbage Collection (GC)
tab-dev-clear-playtime = Reset Server Playtime
tab-dev-clear-slots = Reset Watched Slots
tab-dev-clear-news-cache = Clear News Cache
tab-dev-reset-all-cvars = Reset All CVars to Defaults

