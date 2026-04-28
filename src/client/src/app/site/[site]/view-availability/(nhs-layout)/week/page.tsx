import {
  assertPermission,
  fetchSite,
  fetchFeatureFlag,
  fetchPermissions,
} from '@services/appointmentsService';
import { ViewWeekAvailabilityPage } from './view-week-availability-page';
import { endOfUkWeek, startOfUkWeek } from '@services/timeService';
import { notFound } from 'next/navigation';
import fromServer from '@server/fromServer';
import { Heading, Button } from 'nhsuk-react-components';

type PageProps = {
  searchParams?: Promise<{
    date: string;
  }>;
  params: Promise<{
    site: string;
  }>;
};

const Page = async ({ searchParams, params }: PageProps) => {
  const { site: siteFromPath } = { ...(await params) };

  const { date } = { ...(await searchParams) };
  if (date === undefined) {
    notFound();
  }

  await fromServer(assertPermission(siteFromPath, 'availability:query'));

  const [cancelADateRange, site, sitePermissions] = await Promise.all([
    fromServer(fetchFeatureFlag('CancelADateRange')),
    fromServer(fetchSite(siteFromPath)),
    fromServer(fetchPermissions(siteFromPath)),
  ]);

  const canChangeAvailability =
    cancelADateRange.enabled && sitePermissions.includes('availability:setup');

  const ukWeekStart = startOfUkWeek(date);
  const ukWeekEnd = endOfUkWeek(date);

  // Construct the return URL for the current Week View
  // Since date is required for this page, we know it exists here
  const currentViewPath = `/site/${siteFromPath}/view-availability/week?date=${date}`;

  // Encode it for the query string
  const encodedReturnUrl = encodeURIComponent(currentViewPath);

  return (
    <>
      {canChangeAvailability && (
        <ol className="nhsuk-list nhsuk-u-margin-0 nhsuk-button-group-flat flex-row">
          <Button
            href={`/manage-your-appointments/site/${siteFromPath}/change-availability?returnUrl=${encodedReturnUrl}`}
            className="no-print"
            small
            secondarySolid
          >
            Change availability
          </Button>
        </ol>
      )}

      <Heading headingLevel="h2">
        <span className="nhsuk-caption-l">{site.name}</span>
        {`${ukWeekStart.format('D MMMM')} to ${ukWeekEnd.format('D MMMM')}`}
      </Heading>

      <ViewWeekAvailabilityPage
        ukWeekStart={ukWeekStart}
        ukWeekEnd={ukWeekEnd}
        site={site}
      />
    </>
  );
};

export default Page;
