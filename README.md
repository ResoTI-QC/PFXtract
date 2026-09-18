# PFXtract

Application Windows graphique pour extraire les certificats publics contenus dans un fichier `.pfx` ou `.p12`.

## Fonctionnalités

- sélection par boîte de dialogue ou glisser-déposer ;
- prise en charge des PFX/P12 protégés par mot de passe ;
- aperçu du sujet, de l’émetteur, de la validité, de l’empreinte et du type de clé ;
- sélection des certificats à exporter ;
- export en CER (DER binaire), PEM (texte), ou les deux ;
- export de la chaîne complète, y compris les certificats CA présents dans le PFX ;
- export des clés privées en PKCS#8 chiffré AES-256 (`.key`) ;
- fenêtre prête pour les formulaires d’hébergement avec trois blocs séparés `CRT`, `KEY` et `CABUNDLE` et boutons de copie ;
- interface bilingue français/anglais, avec le français sélectionné par défaut ;
- aucun écrasement silencieux des fichiers existants ;
- traitement entièrement local.

## Lancer depuis le code source

```powershell
dotnet run --project .\src\PFXtract\PFXtract.csproj
```

## Compiler

```powershell
dotnet build .\PFXtract.sln -c Release
```

## Exécuter les tests

```powershell
dotnet run --project .\tests\PFXtract.Tests\PFXtract.Tests.csproj -c Release
```

Le mot de passe du PFX est uniquement utilisé en mémoire pour ouvrir le fichier, puis le champ est immédiatement vidé. Les clés sont chargées en mode éphémère afin de ne pas être enregistrées dans le magasin de certificats Windows. Un second mot de passe, choisi au moment de l’export, protège les fichiers de clés privées.

Le mode **Copier CRT / KEY / CA** affiche temporairement une clé privée non chiffrée, car les formulaires d’installation SSL attendent généralement ce format. Cette clé ne doit jamais être envoyée ou conservée dans un emplacement non sécurisé.
