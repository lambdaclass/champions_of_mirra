# Mirra Autobattler

<img src="docs/src/images/Curse_of_Myrra_logo.png" alt="Curse of Myrra logo">

## Table of Contents

- [About](#about)
- [Licensing](#licensing)
- [Requirements](#requirements)
- [Suggested Development Environment](#suggested-development-environment)
- [Project Setup](#project-setup)
- [Unity Setup](#unity-setup)
- [Running the Backend](#running-the-backend)
- [Useful Commands](#useful-commands)
- [Documentation](#documentation)

## About

Welcome to the realm of Curse of Myrra, crafted by LambdaClass.

Curse of Myrra is the inaugural game built on our groundbreaking [Game Backend](https://github.com/lambdaclass/game_backend). This open source backend, meticulously developed by Lambda, ensures seamless and reliable gameplay.

Step into a universe where the destinies of heroes from four planets collide in an epic struggle for the favor of Myrra, a capricious deity known for manipulating entire societies by exploiting their deepest desires. Brace yourself for an immersive journey where every decision matters, and the pursuit of victory comes with the ever-present thrill of unpredictability.

Curse of Myrra is more than a game; it's an adventure into a world where strategy, skill, and a dash of chaos converge. Join the battle and confront the challenges that lie ahead in this captivating and dynamic gaming experience. The stage is set, and the Curse of Myrra awaits—embrace the challenge and become a legend!

<div>
  <div float="center">
    <img src="docs/src/images/Curse_of_Myrra_3D_Assets_Muflus.png" alt="Muflus 3D model" width=300px>  
    <img src="docs/src/images/Curse_of_Myrra_3D_Assets_Uma.jpeg" alt="Uma 3D model" width=300px> 
  </div>
  <div float="center">
    <img src="docs/src/images/Curse_of_Myrra_concept_art_Shinko.png" alt="Shinko hero concept art" width=300px>
    <img src="docs/src/images/Curse_of_Myrra_concept_art_Otobi_dog.png" alt="Concept art for a gang member dog in the planet of Otobi" width=300px>
  </div>
<div>

## Licensing

The code is licensed under the Apache 2 license, while the music and graphics are licensed under a CC attribution and share-alike license.

Find our open source 3D models, concept art, music, lore and more in our [Curse of Myrra Open Game Assets](https://github.com/lambdaclass/curse_of_myrra_assets) repository.

## Requirements

Ensure you have the following dependencies installed:

- **Rust:**
  - [Install Rust](https://www.rust-lang.org/tools/install)
- **Elixir and Erlang:**
  - [Install Elixir using ASDF](https://thinkingelixir.com/install-elixir-using-asdf/)
  - Erlang/OTP 26
  - Elixir 1.15.4
- **Docker**
- **Unity:**
  - [Download Unity](https://unity.com/unity-hub)
  - Download the following Unity Store assets 
    - [RPG & MMO UI 6](https://assetstore.unity.com/packages/2d/gui/rpg-mmo-ui-6-99450)
    - [Map Maker](https://assetstore.unity.com/packages/2d/environments/map-maker-249063)
    - [RPG inventory icons](https://assetstore.unity.com/packages/2d/gui/icons/rpg-inventory-icons-56687#version-current)
    - [Basic RPG Icons](https://assetstore.unity.com/packages/2d/gui/icons/basic-rpg-icons-181301)
    - [Resource Vector Graphics](https://assetstore.unity.com/packages/2d/gui/icons/resource-icons-101998)
    - Import the necessary TextMeshPro assets by going to:
      - "Window -> TextMeshPro -> Import TMP Essential Resources" and
      - "Window -> TextMeshPro -> Import TMP Examples and Extras"
 - **Docker**


## Suggested Development Environment

Set up your environment with the following steps:

- Download the [.NET SDK](https://dotnet.microsoft.com/es-es/download/dotnet/thank-you/sdk-7.0.403-macos-arm64-installer) for your operating system.
- In VSCode, download the .NET extension. After installation, change the version to 1.26 (locate the version dropdown next to the Uninstall button in the extension window).
- To check if the setup is successful, go to VSCode's console, select the Output tab, and pick Omnisharp Log in the dropdown. If there are no error logs and Omnisharp is scanning the project, the configuration is correct.

## Project Setup

Ensure Docker is running and execute the following commands:

```bash
git clone https://github.com/lambdaclass/curse_of_myrra
cd curse_of_myrra/server
make db
make setup
```

## Unity Setup

- In Unity Hub, click on the add project button and select the `client` folder.
- Choose the correct editor version and download the [Top Down Engine](https://assetstore.unity.com/packages/templates/systems/topdown-engine-89636) by [More Mountains](https://moremountains.com). Include it in the `Assets/ThirdParty` folder after purchasing the license.
- To test the game, select the scene in `Assets/Scenes/TitleScreen` and run it by clicking the play button.

## Running the Backend

Ensure Docker is running and execute:

```bash
make start
```

For local testing using the [game backend](https://github.com/lambdaclass/game_backend), temporarily edit the `mix.exs` file to point to your _local_ copy of the game backend, for example:
`{:game_backend, path: "/Users/MyUsername/lambda/game_backend"}`

For testing using a remote server, point to the _GitHub URL_ instead and specify the desired branch like so:
`{:game_backend, git: "https://github.com/lambdaclass/game_backend", branch: "main"}`

Also, you may find it useful to create two users in the database. One of them should have "user1" as the device_client_id and the other one should have "user2" as the device_client_id. This will allow you to test the battle using the backend.

## Useful Commands

```bash
make tests
```

Will run Elixir and Rust tests

```bash
make format
```

Will format Elixir and Rust code.

```bash
make prepush
```

Will format your code and run Credo check and tests.

## Documentation

Explore our documentation [here](https://docs.curseofmyrra.com/) or run it locally. To run locally, install:

```
cargo install mdbook
cargo install mdbook-mermaid
```

Then run:

```
make docs
```

Open: [http://localhost:3000/](http://localhost:3000/ios_builds.html)

Some key documentation pages:

- [Backend architecture](https://docs.curseofmyrra.com/backend_architecture.html)
- [Message protocol](https://docs.curseofmyrra.com/message_protocol.html)
- [Android build](https://docs.curseofmyrra.com/android_builds.html)
- [IOs builds](https://docs.curseofmyrra.com/ios_builds.html)

## Contact and Socials

If you have any questions, feedback, or comments:

- **Email:** gamedev@lambdaclass.com

We share our development and creative process in the open, follow us for frequent updates on our game:

- **Twitter:** [@CurseOfMirra](https://twitter.com/curseofmirra)
- **Reddit:** [r/curseofmirra](https://www.reddit.com/r/curseofmirra/)
- **Discord:** [join link](https://discord.gg/hxDRsbCpzC)
- **Telegram:** [t.me/curseofmirra](https://t.me/curseofmirra)
