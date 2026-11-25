# Salasanaton kirjautuminen

*Päätetty ja toteutettu 25.11.2026.*

Kirjautuminen toimii kuten Slackissa: käyttäjä kirjoittaa sähköpostinsa, saa koodin sähköpostiin ja kirjoittaa sen. Salasanoja ei ole. Google-kirjautuminen jää ennalleen.

## 1. Kirjautuminen

1. **Sähköpostisivu.** Käyttäjä kirjoittaa sähköpostinsa ja painaa "Lähetä koodi". Sivu korvaa nykyiset Login- ja Register-sivut. Turnstile ja IP-rajoitus pysyvät.
2. **Koodi.** Auth server lähettää kuusinumeroisen koodin ja linkin. Sivu sanoo aina "Lähetimme koodin", jotta sivulta ei voi päätellä, kenellä on tunnus. Linkki avaa koodisivun koodi valmiiksi täytettynä, ja käyttäjä painaa "Kirjaudu". Linkki ei kirjaa sisään suoraan, koska sähköpostipalveluiden tietoturvatarkistukset avaavat linkkejä automaattisesti ja käyttäisivät koodin.
3. **Koodisivu.** Käyttäjä kirjoittaa koodin.
   - Jos käyttäjä on olemassa, hän kirjautuu sisään.
   - Jos ei ole, käyttäjä luodaan ja hän kirjautuu sisään. Käyttäjä luodaan vasta oikealla koodilla, joten pelkkä sähköpostin kirjoittaminen ei luo mitään.
4. **Paluu tuotteeseen** `returnUrl`:n kautta kuten nyt.

Tuotteen `prompt=create` (rekisteröityminen) avaa saman sivun, otsikkona "Luo tunnus".

## 2. Koodin säännöt

- Kuusi numeroa, voimassa 30 minuuttia.
- Enintään 10 yritystä, sen jälkeen koodi mitätöityy ja käyttäjä pyytää uuden. Arvaamalla onnistuu siis 1/100 000 todennäköisyydellä koodia kohden.
- Uusi koodi mitätöi saman sähköpostin edellisen.
- Koodi on sidottu sähköpostiin, ei käyttäjään, koska uutta käyttäjää ei vielä ole.
- Vain koodin tiiviste tallennetaan, kuten nykyisissä linkeissä.
- Lukitulle käyttäjälle ei lähetetä koodia.

Kehityksessä koodi näkyy lokissa (`LoggingUserEmailSender`), kuten linkit nyt.

## 3. Kutsu

- `POST api/user/invite` luo käyttäjän (tila Invited) ja roolin kuten nyt.
- Sähköpostissa on linkki kirjautumissivulle, sähköposti valmiiksi täytettynä. Linkissä ei ole tunnistetta.
- Käyttäjä kirjautuu koodilla, ja tila muuttuu Activeksi.
- Käyttämättömien kutsujen siivous jää, mutta sen sääntö ei enää katso salasanalinkkejä.

## 4. Mitä poistuu

- Salasana käyttäjältä (`PasswordHash`) ja salasanan tarkistus.
- Lukitus väärien salasanojen jälkeen (`FailedLogins`, `LockedUntil`). Ylläpitäjän lukitus (`Locked`) jää.
- Sivut SetPassword, PasswordSet ja ForgotPassword.
- Salasanalinkit (`PasswordToken`) ja niiden sähköpostit.
- Admin-UI:n salasanan vaihto ja "Salasana asetettu" -tieto.
- Seed-käyttäjien salasanat.

## 5. Kirjautumisen kesto (tehty 25.11.2026)

Auth serverin eväste säilyy selaimen sulkemisen yli ja kestää 90 päivää viimeisestä käynnistä (`UserSession.Lifetime`). Koodia kysytään siis vain, jos auth serverillä ei ole käyty 90 päivään.

## 6. Toteutusjärjestys

1. Koodilla kirjautuminen salasanan rinnalle, jotta sitä voi kokeilla.
2. Kutsu käyttämään kirjautumissivua.
3. Salasanojen poisto (kohta 4).
