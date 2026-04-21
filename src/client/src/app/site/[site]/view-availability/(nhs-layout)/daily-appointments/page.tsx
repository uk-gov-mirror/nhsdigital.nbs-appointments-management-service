import {
  assertPermission,
  fetchPermissions,
  fetchSite,
  fetchClinicalServices,
  fetchFeatureFlag,
} from '@services/appointmentsService';
import { fetchBookings } from '../../../../../lib/services/appointmentsService';
import { FetchBookingsRequest } from '@types';
import {
  dateTimeFormat,
  parseToUkDatetime,
  GetCurrentDateTime,
} from '@services/timeService';
import { notFound } from 'next/navigation';
import fromServer from '@server/fromServer';
import PrintPageButton from '@components/print-page-button';
import { Heading, Button } from 'nhsuk-react-components';
import { DayView } from './day-view';

type PageProps = {
  searchParams?: Promise<{
    date: string;
    page: number;
    tab?: string;
  }>;
  params: Promise<{
    site: string;
  }>;
};

const Page = async ({ params, searchParams }: PageProps) => {
  const { site: siteFromPath } = { ...(await params) };
  const { date, page, tab } = { ...(await searchParams) };
  if (date === undefined || page === undefined) {
    return notFound();
  }

  await fromServer(assertPermission(siteFromPath, 'booking:view-detail'));

  const fromDate = parseToUkDatetime(date);
  const toDate = fromDate.endOf('day');

  const fetchBookingsRequest: FetchBookingsRequest = {
    from: fromDate.format(dateTimeFormat),
    to: toDate.format(dateTimeFormat),
    site: siteFromPath,
  };

  const [site, bookings, clinicalServices, cancelADateRange, sitePermissions] =
    await Promise.all([
      fromServer(fetchSite(siteFromPath)),
      fromServer(fetchBookings(fetchBookingsRequest, ['Booked', 'Cancelled'])),
      fromServer(fetchClinicalServices()),
      fromServer(fetchFeatureFlag('CancelADateRange')),
      fromServer(fetchPermissions(siteFromPath)),
    ]);

  const canCancelBookings = sitePermissions.includes('booking:cancel');

  const canChangeAvailability =
    cancelADateRange.enabled && sitePermissions.includes('availability:setup');

  // Construct the return URL for the current Day View
  // We include date, page, and tab so the user returns to exactly what they were seeing
  const currentViewPath = `/site/${siteFromPath}/view-availability/daily-appointments?date=${date}&page=${page}${tab ? `&tab=${tab}` : ''}`;

  // Encode it for safe URL passing
  const encodedReturnUrl = encodeURIComponent(currentViewPath);

  return (
    <>
      <ol className="nhsuk-list nhsuk-u-margin-0 nhsuk-button-group-flat flex-row">
        {canChangeAvailability && (
          <Button
            href={`/manage-your-appointments/site/${siteFromPath}/change-availability?returnUrl=${encodedReturnUrl}`}
            className="no-print"
            small
            secondarySolid
            style={{
              marginRight: '1rem',
            }}
          >
            Change availability
          </Button>
        )}
        &nbsp;
        <PrintPageButton />
      </ol>

      <Heading headingLevel="h2">
        <span className="nhsuk-caption-l">{site.name}</span>
        {fromDate.format('dddd D MMMM YYYY')}
      </Heading>

      <p className="print-out-data" aria-hidden="true">
        Generated: {GetCurrentDateTime()}
      </p>

      <DayView
        fromDate={fromDate}
        bookings={bookings}
        canCancelBookings={canCancelBookings}
        clinicalServices={clinicalServices}
        site={site}
      />
    </>
  );
};

export default Page;
