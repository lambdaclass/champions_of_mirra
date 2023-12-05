# Curse of Myrra

<img src="docs/src/images/Curse_of_Myrra_logo.png" alt="Curse of Myrra logo">

## Table of Contents

- [About](#about)
- [Licensing](#licensing)
- [Requirements](#requirements)
- [Suggested Development Environment](#suggested-development-environment)
- [Project and Unity Setup](#project-and-unity-setup)
- [Running the Backend](#running-the-backend)
- [Documentation](#documentation)
- [Contact and Socials](#contact-and-socials)

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
- **Unity:**
  - [Download Unity](https://unity.com/unity-hub)
- **Docker**

## Suggested Development Environment

Set up your environment with the following steps:

- Download the [.NET SDK](https://dotnet.microsoft.com/es-es/download/dotnet/thank-you/sdk-7.0.403-macos-arm64-installer) for your operating system.
- In VSCode, download the .NET extension. After installation, change the version to 1.26 (locate the version dropdown next to the Uninstall button in the extension window).
- To check if the setup is successful, go to VSCode's console, select the Output tab, and pick Omnisharp Log in the dropdown. If there are no error logs and Omnisharp is scanning the project, the configuration is correct.

## Project and Unity Setup
- Open a terminal and clone the project:

```bash
git clone https://github.com/lambdaclass/curse_of_myrra
```

- In Unity Hub, click on the add project button and select the `curse_of_myrra/client` folder.
- Choose the correct editor version and download the [Top Down Engine](https://assetstore.unity.com/packages/templates/systems/topdown-engine-89636) by [More Mountains](https://moremountains.com). Include it in the `Assets/ThirdParty` folder after purchasing the license.
- To test the game, select the scene in `Assets/Scenes/TitleScreen` and run it by clicking the play button.

## Running the Backend

For local testing, use the [game backend](https://github.com/lambdaclass/game_backend). Ensure Docker is running and execute:

```bash
git clone https://github.com/lambdaclass/game_backend
make db
make setup
make start
```
Remember to set ```localhost``` as the server.

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

- [Message protocol](https://docs.curseofmyrra.com/message_protocol.html)
- [Android build](https://docs.curseofmyrra.com/android_builds.html)
- [IOs builds](https://docs.curseofmyrra.com/ios_builds.html)

## Contact and Socials

If you have any questions, feedback, or comments:

- **Email:** gamedev@lambdaclass.com

We share our development and creative process in the open, follow us for frequent updates on our game:

- **Twitter:** [@CurseOfMyrra](https://twitter.com/curseofmyrra)
- **Reddit:** [r/curseofmyrra](https://www.reddit.com/r/curseofmyrra/)
- **Discord:** [join link](https://discord.gg/hxDRsbCpzC)
- **Telegram:** [t.me/curseofmyrra](https://t.me/curseofmyrra)
