# Drone CI plugin for Stream Deck

## Usage

- Build the project with Visual Studio
- Copy the `bin/Release/com.drosocode.droneci.sdPlugin` folder in `%appdata%\Elgato\StreamDeck\Plugins`

## Configuration

- URL is your drone url (ex: `http://192.168.1.20:4000`)
- Token is your access token (can be found in drone user settings)
- Update interval is in minutes
- Mode can be status (displays ok/progress/error icons depending on the latest build status, click will force the update) or promote (displays a promote icon if the build hasn't already been promoted, click will promote the latest build to the specified target)
- Owner is the repo owner
- Name is the repo name
- Target is the promote target (only needed in promote mode)
