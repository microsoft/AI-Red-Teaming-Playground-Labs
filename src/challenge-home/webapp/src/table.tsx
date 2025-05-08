// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  Button,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableCellLayout,
  TableColumnDefinition,
  TableColumnId,
  TableHeader,
  TableHeaderCell,
  TableRow,
  createTableColumn,
  useTableFeatures,
  useTableSort
} from "@fluentui/react-components";
import {
  DocumentRegular, OpenRegular
} from "@fluentui/react-icons";
import * as React from "react";

type Item = {
  id: number;
  name: string;
  description: string;
  category: string;
  url?: string;
};

const columns: TableColumnDefinition<Item>[] = [
    createTableColumn < Item > ({
    columnId: "id",
    compare: (a, b) => {
      return a.id - b.id;
    },
  }),
  createTableColumn < Item > ({
    columnId: "name",
    compare: (a, b) => {
      return a.name.localeCompare(b.name);
    },
  }),
  createTableColumn < Item > ({
    columnId: "description",
    compare: (a, b) => {
      return a.description.localeCompare(b.description);
    },
  }),
  createTableColumn < Item > ({
    columnId: "category",
    compare: (a, b) => {
      return a.category.localeCompare(b.category);
    },
  }),
];

export const SortControlled = () => {
  const [items, setItems] = React.useState<Item[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);
  const [sortState, setSortState] = React.useState < {
    sortDirection: "ascending" | "descending";
    sortColumn: TableColumnId | undefined;
  } > ({
    sortDirection: "ascending" as const,
    sortColumn: "id",
  });

  const {
    getRows,
    sort: { getSortDirection, toggleColumnSort, sort },
  } = useTableFeatures(
    {
      columns,
      items,
    },
    [
      useTableSort({
        sortState,
        onSortChange: (e, nextSortState) => setSortState(nextSortState),
      }),
    ]
  );

  const headerSortProps = (columnId: TableColumnId) => ({
    onClick: (e: React.MouseEvent) => toggleColumnSort(e, columnId),
    sortDirection: getSortDirection(columnId),
  });

  const rows = sort(getRows());

  React.useEffect(() => {
    fetch('/data.json')
      .then(response => response.json())
      .then(data => {
        setItems(data);
        setIsLoading(false);
      });
  }, []);

  const openChallenge = (id: number, url?: string) => {

    const getUrl = (id: number, url?: string) => {
      if (url) {
        return url;
      } else {
        return `/challenge/${id}/`;
      }
    };
    
    //Check if cookie needs to open in a new tab
    if (document.cookie.includes("home-new-tab=true")) {
      window.open(getUrl(id, url), '_blank');
    } else {
      window.location.href = getUrl(id, url);
    }
  }

  return (
    <div>
      {isLoading ? (
        <Spinner label="Loading..." />
      ) : (
          <div className="table-container">
            <Table sortable aria-label="Table with controlled sort">
              <TableHeader>
                <TableRow>
                  <TableHeaderCell id="table-header-id" {...headerSortProps("id")}>Id</TableHeaderCell>
                  <TableHeaderCell id="table-header-name" {...headerSortProps("name")}>Name</TableHeaderCell>
                  <TableHeaderCell id="table-header-description" {...headerSortProps("description")}>
                    Description
                  </TableHeaderCell>
                  <TableHeaderCell id="table-header-category" {...headerSortProps("category")}>
                    Category
                  </TableHeaderCell>
                  <TableHeaderCell></TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map(({ item }, i) => (
                  <TableRow key={i}>
                    <TableCell>{item.id}</TableCell>
                    <TableCell >
                      <TableCellLayout media={<DocumentRegular/>}>
                        {item.name}
                      </TableCellLayout>
                    </TableCell>
                    <TableCell>{item.description}</TableCell>
                    <TableCell>{item.category}</TableCell>
                    <TableCell role="gridcell">
                      <TableCellLayout>
                        <Button icon={<OpenRegular />} onClick={(e: React.MouseEvent) => { openChallenge(item.id, item.url) }}>Launch</Button>
                      </TableCellLayout>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
      )}
    </div>
  );
};