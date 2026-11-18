 Nosferatu Engine – AI-Driven Minecraft NPC Bot

An autonomous, AI-powered NPC system for Minecraft servers built with Python, Google's Gemini API, and RCON. 
Count Nosferatu can interact with players in real-time, manages a persistent reputation system, gives quests, and 
triggers dynamic, real-time in-game events and server actions based on AI decisions.".



 Key Features
- AI-Powered Roleplay: Powered by Google Gemini API using structured JSON output (`response_schema`).
- Dynamic Reputation System: Tracks standing (-300 to +100) per player, driving different tiers of NPC behavior.
- In-Game Action Execution: Triggers lightning strikes, mob ambushes, status effects, and item rewards via RCON.
- Asynchronous Architecture: Non-blocking log streaming and async thread execution for zero server lag.



Setup & Installation Guide

  Minecraft Server Requirements
1.Use PaperMC (1.20+ recommended).
2. Enable RCON in your `server.properties`:
   ```properties
   enable-rcon=true
   rcon.port=25575
   rcon.password=YOUR_RCON_PASSWORD