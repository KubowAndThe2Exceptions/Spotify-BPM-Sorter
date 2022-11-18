# Spotify-BPM-Sorter
This console application (UI coming soon) uses Spotify's API to call a list of tracks from a target playlist, sort them by Beats Per Minute in descending order,
and place them into separate playlists that represent ranges of BPM.  The motivation for this project came from a problem I encountered as a DJ who regularly 
makes playlists for dances: I dont know how fast or slow my songs are, or how many fast or slow songs I have.

### What I learned from this project so far:
- How to make calls to an API using a library
- HTTP response status codes and what they are
- Expanded on my knowledge of storing and retrieving information from databases
- How to upgrade database versions
- How to code asynchronously

### Features planned for the future:
- UI (via WPF)
- Simple features to edit incorrect BPM before a track is stored in the database
- A means of removing individual tracks from the database
- A means of removing tracks from the database AND target playlist
- Remove the need to make a physical browser for spotify log-in purposes
- Ability to choose the target playlists and change bpm range representations
- Playlist builder and controlled music playback, perhaps a way to fade music in and out automatically if possible
