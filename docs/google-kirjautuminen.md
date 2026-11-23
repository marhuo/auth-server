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

### Googlen tiedot erotetaan omasta logiikasta

Google-kirjautumisen jälkeen tarvitaan vain neljä tietoa:

```csharp
public record ExternalIdentity(string Provider, string ProviderUserId, string Email, bool EmailVerified);
```

Kaikki oma logiikka on yhdessä metodissa, joka ei tiedä Googlesta mitään:

```csharp
// UserAuthenticationService
public async Task<User?> SignInExternalAsync(ExternalIdentity identity)
// etsi user_external_ids → yhdistä sähköpostilla → luo uusi käyttäjä → tarkista lukitus
```

Googlen paluukäsittelijä vain lukee tiedot Googlen vastauksesta ja kutsuu metodia. Sama metodi toimii myöhemmin myös muille palveluille (esim. Microsoft).

### Testisivu kehitykseen

Kehityksessä on sivu "Kirjaudu testikäyttäjänä", jolla on kentät `sub`, `email` ja `email_verified`. Se kutsuu **samaa** `SignInExternalAsync`-metodia ja jatkaa `returnUrl`:iin. Login-sivulla näkyy kehityksessä nappi "Kirjaudu (testi-Google)".

```
Tuotanto:  Login → Google → paluukäsittelijä ─┐
                                              ├→ SignInExternalAsync → kirjaudu sisään → returnUrl
Kehitys:   Login → testisivu (lomake) ────────┘
```

- Ei Dockeria eikä Google-tiliä.
- Reunatapaukset on helppo tuottaa: sama sähköposti eri `sub`:lla, lukitun käyttäjän sähköposti, `email_verified = false`.
- Testisivu ja -nappi ovat olemassa vain, kun `IsDevelopment()` on tosi. Tuotannossa sivua ei ole.

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

- **Yhdistetäänkö olemassa olevaan käyttäjään sähköpostin perusteella?** Esim. `matti@example.com` on jo olemassa salasanalla ja kirjautuu Googlella ensimmäistä kertaa samalla osoitteella.
  - `email_verified = true` tarkoittaa, että osoite oli tilin omistajan hallussa vahvistushetkellä. Gmail-osoitteita ei kierrätetä, mutta muita voidaan: yritys antaa osoitteen uudelle työntekijälle, domain vanhenee ja ostetaan uudelleen, tai Workspace-ylläpitäjä luo osoitteen. `false`-tapauksessa ei koskaan yhdistetä.
  - **A:** yhdistetään automaattisesti, kun `email_verified = true`. Helpoin, mutta luottaa Googlen vanhaan vahvistukseen.
  - **B:** yhdistetään vasta, kun käyttäjä todistaa omistavansa olemassa olevan käyttäjän: sähköpostilinkki nykyiseen osoitteeseen (toimii myös kutsutuille ilman salasanaa) tai vanha salasana. Keycloak toimii näin, Auth0 ei yhdistä oletuksena lainkaan.
  - Suositus: **B sähköpostilinkillä**, uusi tyyppi `PasswordToken`-mekanismiin. Ei vielä päätetty.
  - Riski on vain ensimmäisellä yhdistämiskerralla: `sub` kuuluu Google-tilille, joten kierrätetyn osoitteen uudella haltijalla on eri `sub`.
  - Liittyy ratkaisemattomaan ongelmaan kierrätetyistä osoitteista: [saas-myynti-ja-rekisteroityminen.md](saas-myynti-ja-rekisteroityminen.md), osio 12.
- ~~Luodaanko uusi käyttäjä automaattisesti?~~ **Päätetty (23.11.2026): kyllä.** Tuntematon Google-käyttäjä saa tunnuksen heti, koska Google on vahvistanut sähköpostin. Kuka tahansa voi jo rekisteröityä sähköpostilla, joten Google on vain nopeampi tapa. Google-nappi on sekä kirjautuminen että rekisteröityminen, eikä `prompt=create` muuta sitä. Uudella käyttäjällä ei ole tenanttia, joten tuote pyytää luomaan sellaisen.
- **Käyttäjän tila:** `User.Status` on nyt `Active` vain, jos käyttäjällä on salasana (`HasPassword`). Google-käyttäjällä ei ole salasanaa, joten sääntö muuttuu (esim. salasana tai ulkoinen tunnus).
- Voiko Google-käyttäjä myöhemmin asettaa myös salasanan ("Unohtunut salasana")?

## 6. Testaus

Pääosin kehityksen testisivulla (osio 3), ilman Googlea. Oikealla Google-tilillä kokeillaan kerran, että Googlen kirjautumissivu ja paluu toimivat. Google sallii `http://localhost`-paluuosoitteen ilman https:ää.

Tapaukset (testisivulla, ellei toisin mainita):

- Uusi käyttäjä kirjautuu Googlella.
- Olemassa oleva käyttäjä samalla sähköpostilla (riippuu päätöksestä 5).
- Lukittu käyttäjä ei pääse sisään.
- Käyttäjä, jolla on useita tenantteja: tenantin valinta Google-kirjautumisen jälkeen.
- Käyttäjä ilman tenanttia: sovellus pyytää luomaan tenantin.
- `email_verified = false` olemassa olevan käyttäjän sähköpostilla (riippuu päätöksestä 5).
- Peruutus Googlen sivulla palauttaa Login-sivulle (oikealla Google-tilillä).

## 7. Työmäärä

- Google-asetukset ja `AddGoogle`: muutama tunti.
- `user_external_ids`, käyttäjän luonti ja yhdistäminen, tila ja reunatapaukset: 1–2 päivää.
