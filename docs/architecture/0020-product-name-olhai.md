# 0020 — Product name: Olhai

**Context.** The product was built under the name InspectFlow. It is now called **Olhai**. The old name appeared in two kinds of places:

- **What people see:** the logo next to the mark, page titles, a few interface texts, the sandbox checkout, the copyright lines, and the PDF report (logo, page footer and document metadata).
- **What only the code sees:** C# namespaces and assemblies (`InspectFlow.*`), the database and its user (`inspectflow`), Docker images and containers, configuration keys and the AI writing guide's resource name.

**Decision.**

- Everything people see says **Olhai**. Each side reads the name from one place:
  - Web: `PRODUCT_NAME` in `frontend/src/lib/brand.ts`. It feeds the `Brand` logo used in the sidebar, the phone drawer, the auth pages and the public header and footer, the `… · Olhai` title template, the share preview name and the copyright lines.
  - PDF: `PdfTheme.ProductName` for the logo, the page footer and the document's `Creator`.
  - Interface texts that name the product (EN and PT-BR) say Olhai. In Portuguese it is masculine: "o Olhai".
- **InspectFlow stays as the internal code name.** Renaming namespaces, the database, containers and volumes would touch every file and existing deployments (volume and database names) without changing anything a user sees.
- The logo mark (a roofline over a check) is unchanged.

**Consequences.**

- A future rename of what users see means changing two constants and the message catalogs.
- Developers still see `InspectFlow` in code, logs and Docker. The README says so at the top.
- `Report_carries_the_product_name_olhai_not_the_internal_code_name` checks that a finished report says Olhai and never the code name. `e2e/landing.mjs` checks the logo in the header and the page title.
