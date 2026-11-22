# Google-kirjautuminen: suunnitelma

Tila: suunnitelma, ei toteutettu (23.11.2026).

## 1. Mitä tehdään

Auth serverin Login-sivulle tulee nappi **Kirjaudu Googlella**. Google on auth serverin ulkoinen tunnistaja: Google kertoo, kuka käyttäjä on, ja auth server tekee tokenit kuten ennenkin.

Sovellukset (React-demo, Razor Pages -demo, tuotteet) eivät muutu. Ne puhuvat edelleen vain auth serverin kanssa.

```
Sovellus → auth server /connect/authorize → Login-sivu → Google → auth server /signin-google → sovellus
```

## 2. Google Cloud Console

1. Luo projekti ja OAuth-client, tyyppi **Web application**.
2. Paluuosoite (Authorized redirect URI): `http://localhost:5028/signin-google`. Tuotannossa auth serverin oma osoite.
3. Suostumussivu (OAuth consent screen) tilaan **Testing** ja testikäyttäjiksi omat Google-tilit. Googlen tarkistusta ei silloin tarvita.
4. Client id ja secret auth serverin user-secretsiin, ei `appsettings.json`:iin.

## 3. Koodi

- NuGet-paketti `Microsoft.AspNetCore.Authentication.Google` ja `AddGoogle(...)` (`AuthenticationSetup`).
- Login-sivulle nappi, joka kutsuu `Challenge("Google")` ja vie `returnUrl`:n mukana.
- Paluun käsittely: luetaan Googlen `sub`, `email` ja `email_verified`, etsitään tai luodaan käyttäjä, tarkistetaan `User.CanSignIn` ja kirjataan sisään `UserSession.SignInAsync`:lla. Sen jälkeen jatketaan `returnUrl`:iin, eli tenantin valinta ja tokenit toimivat kuten salasanakirjautumisessa.

## 4. Taulu `user_external_ids`

Ulkoisen palvelun id:n mäppäys auth serverin käyttäjään. Domain-malli `UserExternalId`, EF-entiteetti `UserExternalIdEntity`.

| provider | provider_user_id | user_id |
|---|---|---|
| google | 104857392018475 | Matin id |
| google | 209384756102938 | Liisan id |
| microsoft | a1b2c3-... | Matin id |

- Käyttäjä tunnistetaan **`provider_user_id`:llä** (Googlen `sub`), ei sähköpostilla. Sähköposti voi vaihtua Googlessa, `sub` ei.
- Erillinen taulu, koska samalla käyttäjällä voi olla useita ulkoisia tunnuksia.
- Uniikki: (`provider`, `provider_user_id`).
- Plus-osoitteet eivät koske Google-kirjautumista: Google palauttaa aina tilin pääosoitteen (`jaana@gmail.com`), ja käyttäjä tunnistetaan `sub`:lla. Yksi Google-tili on aina yksi käyttäjä. Sähköpostirekisteröitymisessä plus-osoitteet ovat eri käyttäjiä (ks. [saas-myynti-ja-rekisteroityminen.md](saas-myynti-ja-rekisteroityminen.md), osio 11).

## 5. Avoimet päätökset

- **Yhdistetäänkö olemassa olevaan käyttäjään sähköpostin perusteella?** Esim. `matti@example.com` on jo olemassa salasanalla ja kirjautuu Googlella samalla osoitteella. Turvallista vain, jos Google ilmoittaa `email_verified = true`.
- ~~Luodaanko uusi käyttäjä automaattisesti?~~ **Päätetty (23.11.2026): kyllä.** Tuntematon Google-käyttäjä saa tunnuksen heti, koska Google on vahvistanut sähköpostin. Kuka tahansa voi jo rekisteröityä sähköpostilla, joten Google on vain nopeampi tapa. Google-nappi on sekä kirjautuminen että rekisteröityminen, eikä `prompt=create` muuta sitä. Uudella käyttäjällä ei ole tenanttia, joten tuote pyytää luomaan sellaisen.
- **Käyttäjän tila:** `User.Status` on nyt `Active` vain, jos käyttäjällä on salasana (`HasPassword`). Google-käyttäjällä ei ole salasanaa, joten sääntö muuttuu (esim. salasana tai ulkoinen tunnus).
- Voiko Google-käyttäjä myöhemmin asettaa myös salasanan ("Unohtunut salasana")?

## 6. Testaus

Käsin, oikealla Google-tilillä. Google sallii `http://localhost`-paluuosoitteen ilman https:ää. Googlen kirjautumissivua ei voi ajaa automaattisissa testeissä.

Tapaukset:

- Uusi käyttäjä kirjautuu Googlella.
- Olemassa oleva käyttäjä samalla sähköpostilla (riippuu päätöksestä 5).
- Lukittu käyttäjä ei pääse sisään.
- Käyttäjä, jolla on useita tenantteja: tenantin valinta Google-kirjautumisen jälkeen.
- Käyttäjä ilman tenanttia: sovellus pyytää luomaan tenantin.
- Peruutus Googlen sivulla palauttaa Login-sivulle.

## 7. Työmäärä

- Google-asetukset ja `AddGoogle`: muutama tunti.
- `user_external_ids`, käyttäjän luonti ja yhdistäminen, tila ja reunatapaukset: 1–2 päivää.
