# PFXtract

[English](#english) · [Français](#français)

## English

PFXtract is a Windows desktop application for extracting and converting certificates contained in `.pfx` and `.p12` files.

### Download

Download the latest standalone Windows 64-bit executable from the [GitHub releases page](https://github.com/ResoTI-QC/PFXtract/releases/latest), or use the [direct EXE download](https://github.com/ResoTI-QC/PFXtract/releases/latest/download/PFXtract-win-x64.exe). No separate .NET installation is required.

The matching SHA-256 checksum is provided with every release so you can verify the downloaded file.

### Features

- Select files through a dialog or by dragging and dropping them.
- Open password-protected PFX/P12 files.
- Preview the subject, issuer, validity period, thumbprint, and key type.
- Select individual certificates to export.
- Export certificates as CER (binary DER), PEM (text), or both.
- Export the complete certificate chain, including CA certificates stored in the PFX file.
- Export private keys as AES-256-encrypted PKCS#8 files (`.key`).
- Generate separate `CRT`, `KEY`, and `CABUNDLE` blocks ready for SSL hosting forms, with convenient copy buttons.
- Use the bilingual French/English interface; French is selected by default.
- Prevent existing files from being overwritten silently.
- Process everything locally on your computer.

### Run from source

```powershell
dotnet run --project .\src\PFXtract\PFXtract.csproj
```

### Build

```powershell
dotnet build .\PFXtract.sln -c Release
```

### Run the tests

```powershell
dotnet run --project .\tests\PFXtract.Tests\PFXtract.Tests.csproj -c Release
```

### Security

The PFX password is used in memory only to open the file, and the password field is cleared immediately afterward. Keys are loaded ephemerally so they are never saved to the Windows certificate store. A separate password, chosen during export, protects exported private-key files.

The **Copy CRT / KEY / CA** mode temporarily displays an unencrypted private key because SSL installation forms generally require this format. Never share this key or store it in an unsecured location.

---

## Français

PFXtract est une application Windows permettant d’extraire et de convertir les certificats contenus dans des fichiers `.pfx` et `.p12`.

### Télécharger

Téléchargez le dernier exécutable autonome pour Windows 64 bits depuis la [page des versions GitHub](https://github.com/ResoTI-QC/PFXtract/releases/latest), ou utilisez le [téléchargement direct de l’EXE](https://github.com/ResoTI-QC/PFXtract/releases/latest/download/PFXtract-win-x64.exe). Aucune installation séparée de .NET n’est requise.

L’empreinte SHA-256 correspondante accompagne chaque version afin de vérifier le fichier téléchargé.

### Fonctionnalités

- Sélection des fichiers par boîte de dialogue ou glisser-déposer.
- Prise en charge des fichiers PFX/P12 protégés par mot de passe.
- Aperçu du sujet, de l’émetteur, de la période de validité, de l’empreinte et du type de clé.
- Sélection individuelle des certificats à exporter.
- Export des certificats en CER (DER binaire), PEM (texte), ou les deux.
- Export de la chaîne complète, y compris les certificats CA présents dans le fichier PFX.
- Export des clés privées au format PKCS#8 chiffré AES-256 (`.key`).
- Génération de blocs `CRT`, `KEY` et `CABUNDLE` prêts pour les formulaires d’hébergement SSL, avec boutons de copie.
- Interface bilingue français/anglais; le français est sélectionné par défaut.
- Protection contre l’écrasement silencieux des fichiers existants.
- Traitement entièrement local sur votre ordinateur.

### Lancer depuis le code source

```powershell
dotnet run --project .\src\PFXtract\PFXtract.csproj
```

### Compiler

```powershell
dotnet build .\PFXtract.sln -c Release
```

### Exécuter les tests

```powershell
dotnet run --project .\tests\PFXtract.Tests\PFXtract.Tests.csproj -c Release
```

### Sécurité

Le mot de passe du PFX est uniquement utilisé en mémoire pour ouvrir le fichier, puis le champ est immédiatement vidé. Les clés sont chargées en mode éphémère afin de ne jamais être enregistrées dans le magasin de certificats Windows. Un second mot de passe, choisi au moment de l’export, protège les fichiers de clés privées exportés.

Le mode **Copier CRT / KEY / CA** affiche temporairement une clé privée non chiffrée, car les formulaires d’installation SSL exigent généralement ce format. Cette clé ne doit jamais être partagée ou conservée dans un emplacement non sécurisé.
