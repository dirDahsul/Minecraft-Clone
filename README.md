# Minecraft Clone - Unity 3D Project

**Minecraft Clone** is a simple yet somewhat ambitious Unity game built with C#. The project aims to replicate the core mechanics of Minecraft, featuring infinite world generation, block placement, and basic inventory management. This README provides an overview of the project's features, the technical challenges encountered, and the current state of the game.

## 🌍 World Generation

### Procedural Generation
- The game uses **Perlin Noise** to generate a smooth and infinite world.
- World generation is based on a **seed**, ensuring consistent world creation for the same seed.
- The backend relies on a dictionary with `(x, y, z)` coordinates as keys and the associated block as the value.

### Infinite World
- The world is procedurally generated in chunks, each of size `16x16` blocks.
- The world expands infinitely in all directions, with new chunks generated as the player moves.
- View distance is defined by the number of chunks loaded around the player, creating a seamless experience as the player explores.

## 🧱 Block Rendering

### Mesh Generation
- Blocks (or more appropriately, the block's faces) are only rendered if they are visible to the player, determined by checking the dictionary for neighboring blocks.
- Each 'visible' block has its own individual face mesh (front, back, up, down, left, right). Basically the block is made out of meshes for each of it's faces.
- Although this approach allows for detailed, straight-on, point-on block generation and rendering, it results in performance issues due to the large number of individual meshes being rendered.

### Performance Considerations
- The game suffers from significant performance hits due to the use of individual meshes for each block face.
- The rendering method is simple, straight on point and sadly unoptimized, which may cause the game to run slower, especially as the world grows.

## 🛠️ Gameplay Features

### Block Interaction
- Players can pick up and place blocks, interacting with the world similarly to Minecraft.
- The game includes a simple inventory system for managing collected blocks.

### Terrain Features
- The world generation includes basic terrain features like **trees** and **water**.
- Water is generated at lower elevations (based on the `z` value), flooding low-lying areas.
- Currently, the game supports only one biome, referred to as "normal.". No 'forests', 'hills', 'deserts', etc. It's a one do all biome, referred as 'normal'.

## ⚠️ Known Issues & Limitations

- **Performance**: The game is not optimized, resulting in potential lag or slow performance as the world grows.
- **Limited Biomes**: Only one biome type is available. There are no specialized biomes like oceans, deserts, hills, or forests.
- **Diversity**: As the game only has cabblestone, dirt, grass, tree trunk and leaves, it currently lacks the diversity of having many other blocks/entities.
- **Rendering**: The current rendering technique, which involves many individual meshes, is not ideal and significantly impacts performance.
- **Novice-Level Code**: The codebase is functional but not optimized or professionally designed. This project is intended as a learning experience rather than a polished product.

## 🎮 Media

- **Screenshots** and **video** of the gameplay are included right here. These provide a visual overview of the game's current state (which is functional, just not offering full minecraft experience at all).

## 📜 Disclaimer

This project is a **work-in-progress** and serves primarily as a novice-level experiment in game development. It is not intended to be a fully optimized or professional-grade game. The project is an as-is state with no intent to be further developed at the moment.

---

Thank you for taking the time to explore **Minecraft Clone**! While it may not be perfect, it’s a fascinating project that I really enjoyed working on. Especially because I was interested in the infinite-procedural geration of a world that minecraft has! 
