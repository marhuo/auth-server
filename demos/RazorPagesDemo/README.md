# Razor Pages demo

ASP.NET Core Razor Pages -sovellus, joka kirjautuu autentikointipalvelimen kautta (OpenID Connect).

## Konfiguraatio

Kaikki kirjautumiseen liittyvä on näissä:

| Tiedosto | Mitä |
|---|---|
| `appsettings.json` | Auth serverin osoite, client id ja secret |
| `Program.cs`: `AddAuthentication` | Eväste + OpenID Connect -asetukset |
| `Program.cs`: `AddRazorPages` | Mitkä sivut vaativat kirjautumisen tai roolin |
| `Controllers/AccountController.cs` | Kirjautuminen (`Challenge`), rekisteröityminen (`Prompt = "create"`), uloskirjautuminen (`SignOut`) ja tenantin vaihto |

Clientin paluuosoite auth serverissä: `https://localhost:5101/signin-oidc`.

Muut tiedostot ovat demon omia ominaisuuksia (tenantin luonti, kutsut, osto), eivät kirjautumista.

## Käynnistys

VS Code: Run and Debug (Ctrl+Shift+D) → **Auth server + Razor Pages demo** → ▶.

Tai komentoriviltä:
1. `dotnet run --project backend/AuthServer.Web` (http://localhost:5028)
2. `dotnet run --project demos/RazorPagesDemo` (https://localhost:5101)

Kehitysdata (`DevDatabaseSeeder`) luo resurssipalvelimen `razor-pages-demo`, roolit `razor_pages_demo_tenant_admin` ja `razor_pages_demo_user` sekä clientit `razor-pages-demo-web` (kirjautuminen) ja `razor-pages-demo-backend` (demon palvelin kutsuu auth serverin API:a).

Kun luot auth serverin tietokannan uudelleen, käynnistä myös demo uudelleen.

## Kokeile

1. Etusivu → **Kokeile ilmaiseksi** → luo tunnus → anna tenantin nimi. Saat roolin `razor_pages_demo_tenant_admin`.
2. **Käyttäjät** → kutsu toinen sähköposti. Kutsusähköposti näkyy auth serverin lokissa (`Invite for ...`).
3. Kirjaudu toisessa selaimessa kutsutulla sähköpostilla. Kirjautumiskoodi näkyy auth serverin lokissa (`Sign-in code for ...`). Rooli on `razor_pages_demo_user`, eikä Laskutus-sivulla ole Osta-nappia.
4. **Useampi tenantti:** toinen käyttäjä luo oman tenanttinsa ja kutsuu ensimmäisen käyttäjän sinne. Kun ensimmäinen käyttäjä kirjautuu, auth server kysyy, kumpaan tenanttiin hän menee. Sen jälkeen tenantin voi vaihtaa vasemman yläkulman valikosta.
