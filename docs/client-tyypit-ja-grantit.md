# Client-tyypit ja grantit

Auth serverissä on kolme client-tyyppiä. Tyyppi määrää, millä tavalla client saa tokenin (grant) ja tarvitaanko salaisuutta (client secret).

| Tyyppi | Admin-UI:ssa | Grantit | Salaisuus | Tenantti |
|---|---|---|---|---|
| `PublicApplication` | Selain- tai mobiilisovellus | `authorization_code`, `refresh_token` | ei | valinnainen |
| `ConfidentialApplication` | Palvelinsovellus | `authorization_code`, `refresh_token` | pakollinen | valinnainen |
| `ConfidentialMachine` | Järjestelmäintegraatio | `client_credentials` | pakollinen | pakollinen |

Grantit määritellään koodissa tiedostossa `backend/AuthServer.Domain/Clients/ClientGrants.cs`. Niitä ei tallenneta tietokantaan.

## Salaisuus ja grant ovat eri asioita

- **Grant** kertoo, miten token haetaan:
  - `authorization_code`: käyttäjä kirjautuu, ja token kertoo, kuka käyttäjä on.
  - `client_credentials`: käyttäjää ei ole, ja client toimii omissa nimissään.
- **Salaisuudella** client todistaa token-endpointissa, kuka se on. Salaisuutta käytetään kaikilla granteilla, ei pelkästään `client_credentials`-grantilla.

## Public- ja Confidential Application

Grantit ovat samat. Ainoa ero on salaisuus:

- **Selain- tai mobiilisovellus** ei voi pitää salaisuutta, koska kuka tahansa voi lukea sovelluksen koodin. Sitä suojaavat redirect URIt ja PKCE.
- **Palvelinsovellus** pitää salaisuuden palvelimella, ja salaisuus vaaditaan, kun se vaihtaa koodin tokeniksi. Varastetulla koodilla ei saa tokenia ilman salaisuutta.

## Miksi palvelinsovelluksella ei ole `client_credentials`-granttia

`client_credentials`-tokenissa ei ole käyttäjää, joten tenantti otetaan clientin asetuksista. Siksi tenantti on pakollinen järjestelmäintegraatiolle mutta ei palvelinsovellukselle, jonka tenantti tulee kirjautuneelta käyttäjältä. Jos palvelinsovellus saisi myös `client_credentials`-grantin, sen taustatyön tokeneista voisi puuttua tenantti.

Lisäksi kun kirjautuminen ja taustatyö ovat eri clienteilla:

- Taustatyölle voi antaa eri scopet kuin sovellukselle.
- Lokeista näkee, kumpi teki kutsun.
- Toisen voi poistaa käytöstä tai sen salaisuuden voi vaihtaa koskematta toiseen.

**Jos sovellus tarvitsee sekä käyttäjien kirjautumisen että taustatyön, sille luodaan kaksi clientia:**

1. Palvelinsovellus käyttäjien kirjautumista varten.
2. Järjestelmäintegraatio taustatyötä varten. Sille annetaan tenantti ja vain taustatyön tarvitsemat scopet.
