Gravitas — PUN2 Multiplayer Arena
A dark fantasy multiplayer arena game built with Unity and Photon PUN 2, developed as a technical exam submission in 5 days.
Overview
Players compete in a 3-round elimination arena using telekinesis to grab and throw boulders at opponents. Last player standing wins each round. Most kills across all rounds wins the match.
Key Features

Telekinesis Mechanic — Camera-swing momentum based boulder grab and throw
Networked Kill Detection — Ownership-based attribution via PunRPC ensures correct kill credit
Ghost State System — Single-bone ragdoll on death, phase-through collision via layer matrix
3-Round Elimination — Round system with synced countdown, late-join compensation, and scoreboard
5-Player Support — Photon Cloud relay, Asia region, deterministic HSV nameplates per ActorNumber
Optimized — Baked lighting, occlusion culling, static batching, GPU instancing for grass

Tech Stack

Unity (URP)
Photon PUN 2 (Asia Region)
C#
Blender (outsourced assets)

Notes
Built in 5 days as a Junior Unity Game Developer technical examination. First multiplayer project.
