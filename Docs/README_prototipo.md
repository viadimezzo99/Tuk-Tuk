# Prototipo Tuk-Tuk Race: come provarlo

## 1. Impostazioni del progetto (una volta sola)
- **Edit > Project Settings > Player > Other Settings > Active Input Handling**: imposta **Both** (oppure **Input System Package (New)**). Il codice usa il nuovo sistema di input.

## 2. Creare la scena
- Nel menu in alto: **TukTuk > Crea scena prototipo**.
- Unity crea la cartella `Assets/Scenes/Prototype.unity` e i materiali in `Assets/Materials`, e imposta la scena come prima della build.
- Se il menu non compare, aspetta che finisca "Compiling Scripts" e riprova.

## 3. Provare
- Premi **Play**.
- Cambio corsia: frecce sinistra/destra oppure tasti A/D. Su telefono, swipe orizzontale.
- Dopo un incidente premi **R** per ripartire.

## Struttura degli script
- `Scripts/Runtime/TukTukController.cs`: avanzamento, velocità crescente, corsie, input, distanza.
- `Scripts/Runtime/EndlessRoad.cs`: strada infinita che ricicla i tratti e genera ostacoli, con collisione.
- `Scripts/Runtime/CameraFollow.cs`: camera dall'alto in terza persona.
- `Scripts/Editor/PrototypeSceneBuilder.cs`: crea la scena dal menu.

## Prossimi passi
- Sostituire i cubi con modelli cartoon (tuk-tuk, bancarelle, minareti) usando le palette dei riferimenti visivi.
- Aggiungere le corse con i clienti (punto A → B) e il sistema di crediti.
- Passare la piattaforma su Android in **File > Build Profiles** quando il prototipo gira bene su PC.
