import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { V1 } from '../../api/index.ts';

/**
 * Repository for fetching SimpleStats page-view data from the SimpleStats API.
 */
export class SimpleStatsPageViewsRepository extends UmbRepositoryBase {
  requestPageViews(startDate: string, endDate: string) {
    return tryExecute(
      this,
      V1.getUmbracoSimplestatsApiV1PageViews({
        query: { startDate, endDate },
      }),
    );
  }
}

export default SimpleStatsPageViewsRepository;
