import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule } from "@vc-shell/framework";
import "./styles.scss";

// legacy: workspace.SqlQueriesState (list → report view / query details); the never-opened report-execution blade
// (metaFormsService "reportExecution") is covered by the report view's export
export default defineAppModule({
  blades,
  locales,
});

export * from "./pages";
